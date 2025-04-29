using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Extensions.FSM.Editor;
using Extensions.FSM.Base.StateMachine;
using UnityEditor;
using UnityEngine;

namespace Extensions.FSM.Editor
{
    public class FsmEditor : EditorWindow
    {
        [MenuItem("Tools/FSM Editor")]
        public static void OpenWindow()
        {
            GetWindow<FsmEditor>("FSM Editor");
        }
        
        private string customStateDataPath = "Assets/";
        private bool showStateDataCreation = false;
        private const float CONTROL_PANEL_WIDTH = 300f;
        private const float CONTROL_PANEL_PADDING = 10f;
        private bool snapToGrid = true;
        private float gridSize = 20f;
        private string lastEditedStateName;
        private GuiStateData currentlyEditingState;

        #region ZoomControls

        private float zoomScale = 1.0f;
        private const float ZOOM_MIN = 0.5f; // Zoom mínimo
        private const float ZOOM_MAX = 2.0f; // Zoom máximo

        #endregion

        #region Scroll

        private Vector2 scrollPosition = Vector2.zero;
        private Rect contentRect = new Rect(0, 0, 2000, 2000); // Un área grande para moverse


        #endregion

        private bool isDragging;
        private Vector2 dragStart;

        private MyStateMachine targetStateMachine;
        private StateCondition defaultStateCondition;
        private GuiFsmData currentStateMachineGui;

        private Color connectionColor = Color.red;
        private Texture2D connectionTexture;
        void OnEnable()
        {
            connectionTexture = new Texture2D(1, 1);
            connectionTexture.SetPixel(0, 0, Color.white);
            connectionTexture.Apply();

            // Modifica el dibujo de conexión temporal
            if (isCreatingConnection && connectionStartNode != null)
            {
                Handles.BeginGUI();

                // Cambiar color basado en validez
                bool isValidTarget = false;
                if (Event.current.mousePosition != null)
                {
                    foreach (var state in currentStateMachineGui.AllGuiStates)
                    {
                        Rect scaledRect = new Rect(
                            state.Rect.x * zoomScale - scrollPosition.x,
                            state.Rect.y * zoomScale - scrollPosition.y,
                            state.Rect.width * zoomScale,
                            state.Rect.height * zoomScale
                        );

                        if (scaledRect.Contains(Event.current.mousePosition))
                        {
                            isValidTarget = state != connectionStartNode;
                            break;
                        }
                    }
                }

                connectionColor = isValidTarget ? Color.green : Color.red;
                Handles.color = connectionColor;

                Vector3 start = new Vector3(connectionStartNode.Rect.xMax * zoomScale,
                    (connectionStartNode.Rect.y + connectionStartNode.Rect.height / 2) * zoomScale);
                Vector3 end = Event.current.mousePosition + scrollPosition;

                // Línea más gruesa con patrón
                Handles.DrawAAPolyLine(5f, start, end);

                // Dibujar flecha
                Vector3 direction = (end - start).normalized;
                float arrowSize = 15f;
                Vector3 right = Quaternion.Euler(0, 0, 30) * -direction * arrowSize;
                Vector3 left = Quaternion.Euler(0, 0, -30) * -direction * arrowSize;

                Handles.DrawAAPolyLine(3f, end, end + right);
                Handles.DrawAAPolyLine(3f, end, end + left);

                Handles.EndGUI();
            }
        }
        
        private void OnDestroy()
        {
            // Guardar automáticamente al cerrar la ventana
            if (targetStateMachine != null && currentStateMachineGui != null)
            {
                SaveFsmChanges();
            }
        }

        private void OnGUI()
        {
            // Dividir el espacio en dos áreas
            Rect controlPanelRect = new Rect(0, 0, CONTROL_PANEL_WIDTH, position.height);
            Rect workspaceRect = new Rect(CONTROL_PANEL_WIDTH, 0, position.width - CONTROL_PANEL_WIDTH, position.height);

            // Dibujar el panel de control
            DrawControlPanel(controlPanelRect);

            // Dibujar el área de trabajo
            DrawWorkspace(workspaceRect);

            UpdateContentRect();

            // Forzar repintado continuo durante la creación de conexión
            if (isCreatingConnection)
            {
                Repaint();
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }

        private void DrawControlPanel(Rect p_rect)
        {
            GUILayout.BeginArea(p_rect, EditorStyles.helpBox);
            GUILayout.Label("FSM Controls", EditorStyles.boldLabel);

            // Contenido existente del panel de control
            if (GUILayout.Button("Options"))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Reset FSM Data"), false, ResetData);
                menu.ShowAsContext();
            }

            targetStateMachine = (MyStateMachine)EditorGUILayout.ObjectField("State Machine", targetStateMachine,
                typeof(MyStateMachine), false);

            defaultStateCondition = (StateCondition)EditorGUILayout.ObjectField("Default Condition",
                defaultStateCondition, typeof(StateCondition), false);

            if (targetStateMachine == null)
            {
                EditorGUILayout.HelpBox("Please assign a State Machine to edit.", MessageType.Info);
            }
            else if (defaultStateCondition == null)
            {
                EditorGUILayout.HelpBox("Please assign a Default State condition.", MessageType.Info);
            }

            GUILayout.Space(10);
            
            

            snapToGrid = EditorGUILayout.Toggle("Snap to Grid", snapToGrid);
            if (snapToGrid)
            {
                gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
            }
            
            // Sección para la ruta personalizada
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("StateData Storage", EditorStyles.boldLabel);
    
            EditorGUILayout.BeginHorizontal();
            customStateDataPath = EditorGUILayout.TextField("Path", customStateDataPath);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.SaveFolderPanel("Select StateData Folder", customStateDataPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convertir a ruta relativa de Assets
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        customStateDataPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            
            if (GUILayout.Button("Create New State"))
            {
                CreateNewState();
            }

            if (GUILayout.Button("Save Changes"))
            {
                SaveFsmChanges();
            }

            GUILayout.EndArea();
        }

        private void DrawWorkspace(Rect p_rect)
        {
            // Ajustar el contentRect para el área de trabajo
            Rect adjustedContentRect = new Rect(
                contentRect.x,
                contentRect.y,
                contentRect.width + CONTROL_PANEL_WIDTH,
                contentRect.height
            );

            // Ajustar el scroll position para el área de trabajo
            Vector2 adjustedScrollPosition = GUI.BeginScrollView(
                p_rect,
                scrollPosition,
                adjustedContentRect,
                true,
                true
            );

            // Guardar la matriz actual para aplicar transformaciones
            Matrix4x4 originalMatrix = GUI.matrix;

            try
            {
                // Aplicar zoom y desplazamiento
                GUIUtility.ScaleAroundPivot(Vector2.one * zoomScale, Vector2.zero);
                GUI.matrix = Matrix4x4.TRS(
                    new Vector3(-adjustedScrollPosition.x, -adjustedScrollPosition.y, 0),
                    Quaternion.identity,
                    Vector3.one
                );

                // Dibujar los nodos y conexiones
                if (targetStateMachine != null && defaultStateCondition != null)
                {
                    if (currentStateMachineGui == null || targetStateMachine != currentStateMachineGui.TargetFsm)
                    {
                        currentStateMachineGui = new GuiFsmData(targetStateMachine);
                        CreateData();
                    }

                    DrawNodeThree();
                    HandleZoom(Event.current);
                    HandleNodeDragging(Event.current);

                    if (isCreatingConnection && connectionStartNode != null)
                    {
                        DrawTemporaryConnection();
                    }
                }
            }
            finally
            {
                // Restaurar la matriz original
                GUI.matrix = originalMatrix;
                GUI.EndScrollView();
            }
        }

        private void DrawTemporaryConnection()
        {
            if (!isCreatingConnection || connectionStartNode == null) return;

            
            Handles.BeginGUI();
            
            Handles.color = Color.green;
            Vector2 mousePos = Event.current.mousePosition;

            // Calcular posición de inicio (centro derecho del nodo inicial)
            Vector3 startPos = new Vector3(
                CONTROL_PANEL_WIDTH + connectionStartNode.Rect.xMax * zoomScale - scrollPosition.x,
                (connectionStartNode.Rect.y + connectionStartNode.Rect.height * 0.5f) * zoomScale - scrollPosition.y
            );

            // Verificar si el mouse está sobre otro nodo
            bool isValidTarget = false;
            foreach (var state in currentStateMachineGui.AllGuiStates)
            {
                Rect scaledRect = new Rect(
                    CONTROL_PANEL_WIDTH + state.Rect.x * zoomScale - scrollPosition.x,
                    state.Rect.y * zoomScale - scrollPosition.y,
                    state.Rect.width * zoomScale,
                    state.Rect.height * zoomScale
                );

                if (scaledRect.Contains(mousePos))
                {
                    isValidTarget = state != connectionStartNode;
                    break;
                }
            }

            // Cambiar color según validez
            Handles.color = isValidTarget ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);

            // Calcular puntos para la curva Bezier
            Vector3 endPos = mousePos;
            float tangentLength = Mathf.Min(150f, Vector3.Distance(startPos, endPos)) * 0.5f;
            Vector3 startTangent = startPos + Vector3.right * tangentLength;
            Vector3 endTangent = endPos + Vector3.left * tangentLength;

            // Dibujar curva temporal más suave
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Handles.color, null, 3f);

            // Dibujar flecha solo si es un objetivo válido
            if (isValidTarget)
            {
                Vector3 arrowDir = (BezierPoint(0.95f, startPos, endPos, startTangent, endTangent) -
                                  BezierPoint(0.9f, startPos, endPos, startTangent, endTangent));
                arrowDir.Normalize();

                float arrowSize = 12f;
                Vector3 arrowTip = endPos;
                Vector3 arrowLeft = arrowTip + Quaternion.Euler(0, 0, 30) * -arrowDir * arrowSize;
                Vector3 arrowRight = arrowTip + Quaternion.Euler(0, 0, -30) * -arrowDir * arrowSize;

                Handles.DrawAAPolyLine(3f, arrowTip, arrowLeft);
                Handles.DrawAAPolyLine(3f, arrowTip, arrowRight);
            }

            Handles.EndGUI();
        }

        // Modifica UpdateContentRect para tener en cuenta el panel de control
        void UpdateContentRect()
        {
            if (currentStateMachineGui == null || currentStateMachineGui.AllGuiStates == null || currentStateMachineGui.AllGuiStates.Count == 0)
            {
                // Tamaño por defecto cuando no hay nodos
                contentRect = new Rect(CONTROL_PANEL_WIDTH, 0, position.width, position.height);
                return;
            }

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var state in currentStateMachineGui.AllGuiStates)
            {
                minX = Mathf.Min(minX, state.Rect.x);
                minY = Mathf.Min(minY, state.Rect.y);
                maxX = Mathf.Max(maxX, state.Rect.x + state.Rect.width);
                maxY = Mathf.Max(maxY, state.Rect.y + state.Rect.height);
            }

            // Añadir márgenes (100px alrededor del contenido)
            float margin = 100f;
            contentRect = new Rect(
                CONTROL_PANEL_WIDTH,
                0,
                Mathf.Max(maxX - minX + 2 * margin, position.width - CONTROL_PANEL_WIDTH),
                Mathf.Max(maxY - minY + 2 * margin, position.height)
            );
        }

        private void DrawNodeThree()
        {
            // Crear copias de las listas para iterar
            var statesToDraw = new List<GuiStateData>(currentStateMachineGui.AllGuiStates);
            var connectionsToDraw = new List<Tuple<GuiStateData, GuiStateData>>();

            // Primero recolectar todas las conexiones
            foreach (var guiState in statesToDraw)
            {
                for (int j = 0; j < guiState.GuiConnections.Count; j++)
                {
                    connectionsToDraw.Add(new Tuple<GuiStateData, GuiStateData>(guiState, guiState.GuiConnections[j]));
                }
            }

            // Calcular área visible
            Rect visibleArea = new Rect(
                scrollPosition.x / zoomScale,
                scrollPosition.y / zoomScale,
                position.width / zoomScale,
                position.height / zoomScale
            );

            // Dibujar conexiones
            foreach (var connection in connectionsToDraw)
            {
                var from = connection.Item1;
                var to = connection.Item2;

                if (from.Rect.Overlaps(visibleArea) || to.Rect.Overlaps(visibleArea))
                {
                    int index = from.GuiConnections.IndexOf(to);
                    if (index >= 0)
                    {
                        DrawConnection(from, index, to);
                    }
                }
            }

            // Dibujar nodos
            foreach (var guiState in statesToDraw)
            {
                if (guiState.Rect.Overlaps(visibleArea))
                {
                    DrawNode(guiState);
                }
            }
        }

        private void CreateData()
        {
            // Limpiar datos existentes
            currentStateMachineGui.AllGuiStates.Clear();

            // Crear nodos para los estados existentes
            for (int i = 0; i < currentStateMachineGui.AllStates.Count; i++)
            {
                var stateData = currentStateMachineGui.AllStates[i];
                Rect nodeRect = new Rect(50 + 300 * i, 100, 200, 190);

                var guiData = new GuiStateData(
                    stateData,
                    stateData.name,
                    nodeRect,
                    stateData.MyState,
                    stateData.StateConditions,
                    stateData.ExitStates
                );

                currentStateMachineGui.AddGuiState(guiData);
            }

            // Actualizar conexiones
            foreach (var guiState in currentStateMachineGui.AllGuiStates)
            {
                var connections = new List<GuiStateData>();
                foreach (var exitState in guiState.ExitStates)
                {
                    var connectedGuiState = currentStateMachineGui.AllGuiStates
                        .FirstOrDefault(g => g.AssociatedStateData == exitState);
                    if (connectedGuiState != null)
                    {
                        connections.Add(connectedGuiState);
                    }
                }
                guiState.SetGuiConnections(connections);
            }

            UpdateContentRect();
        }

        private GuiStateData connectionStartNode;
        private bool isCreatingConnection;
       private void DrawNode(GuiStateData p_state)
        {
            Rect scaledRect = new Rect(
                CONTROL_PANEL_WIDTH + p_state.Rect.x * zoomScale - scrollPosition.x,
                p_state.Rect.y * zoomScale - scrollPosition.y,
                p_state.Rect.width * zoomScale,
                p_state.Rect.height * zoomScale
            );

            // Estilo del nodo basado en si está siendo editado
            bool isBeingEdited = GUI.GetNameOfFocusedControl() == "StateNameField_" + p_state.UniqueId;
            GUIStyle boxStyle = isBeingEdited ?
                new GUIStyle(EditorStyles.helpBox) { normal = { background = MakeTex(2, 2, new Color(0.1f, 0.3f, 0.5f, 0.2f)) } } :
                EditorStyles.helpBox;

            GUI.Box(scaledRect, "", boxStyle);
            GUILayout.BeginArea(scaledRect);

            // --- SECCIÓN DE NOMBRE DEL ESTADO ---
            EditorGUILayout.LabelField("State Name:", EditorStyles.boldLabel);

            string previousName = p_state.StateName;
            GUI.SetNextControlName("StateNameField_" + p_state.UniqueId);

            // Estilo del campo de texto cuando está siendo editado
            GUIStyle textFieldStyle = isBeingEdited ?
                new GUIStyle(EditorStyles.textField) { normal = { textColor = Color.cyan } } :
                EditorStyles.textField;

            p_state.StateName = EditorGUILayout.TextField(p_state.StateName, textFieldStyle);

            // Mostrar instrucciones cuando se está editando
            if (isBeingEdited)
            {
                GUILayout.Label("Enter: Confirm | Escape: Cancel", EditorStyles.miniLabel);
            }

            // Manejo de eventos de teclado
            if (Event.current.isKey && GUI.GetNameOfFocusedControl() == "StateNameField_" + p_state.UniqueId)
            {
                if (Event.current.keyCode == KeyCode.Return)
                {
                    // Confirmar cambios al presionar Enter
                    if (p_state.StateName != previousName && !string.IsNullOrEmpty(p_state.StateName))
                    {
                        if (p_state.AssociatedStateData != null)
                        {
                            RenameStateAsset(p_state);
                        }
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    // Cancelar cambios al presionar Escape
                    p_state.StateName = lastEditedStateName;
                    currentlyEditingState = null;
                    GUI.FocusControl(null);
                    Event.current.Use();
                    Repaint();
                }
            }
            else if (GUI.GetNameOfFocusedControl() == "StateNameField_" + p_state.UniqueId)
            {
                // Actualizar estado de edición
                if (currentlyEditingState != p_state)
                {
                    lastEditedStateName = previousName;
                    currentlyEditingState = p_state;
                }
            }
            else if (currentlyEditingState == p_state)
            {
                // Confirmar cambios al perder el foco
                currentlyEditingState = null;
                if (p_state.StateName != lastEditedStateName && !string.IsNullOrEmpty(p_state.StateName))
                {
                    if (p_state.AssociatedStateData != null)
                    {
                        RenameStateAsset(p_state);
                    }
                }
            }

            // --- SECCIÓN DE STATE DATA ---
            EditorGUILayout.LabelField("State Data", EditorStyles.boldLabel);

            var previousStateData = p_state.AssociatedStateData;
            p_state.AssociatedStateData = (StateData)EditorGUILayout.ObjectField(
                p_state.AssociatedStateData,
                typeof(StateData),
                false);

            // Botón para crear nuevo StateData si no hay uno asignado
            if (p_state.AssociatedStateData == null)
            {
                if (GUILayout.Button("Create StateData"))
                {
                    CreateStateDataForNode(p_state);
                }
            }
            else
            {
                // Actualizar nombre si cambió el StateData
                if (previousStateData != p_state.AssociatedStateData)
                {
                    p_state.StateName = p_state.AssociatedStateData.name;
                    lastEditedStateName = p_state.StateName;
                }

                // --- SECCIÓN DE COMPORTAMIENTO DEL ESTADO ---
                EditorGUILayout.LabelField("State Behavior", EditorStyles.boldLabel);

                var previousState = p_state.State;
                p_state.State = (MyState)EditorGUILayout.ObjectField(
                    p_state.State,
                    typeof(MyState),
                    false);

                // Actualizar StateMachine si cambió el estado
                if (previousState != p_state.State && p_state.State != null)
                {
                    p_state.AssociatedStateData.MyState = p_state.State;
                    EditorUtility.SetDirty(p_state.AssociatedStateData);
                    EditorUtility.SetDirty(targetStateMachine);
                }
            }

            // --- BOTONES DE ACCIÓN ---
            if (GUILayout.Button("Add Connection"))
            {
                connectionStartNode = p_state;
                isCreatingConnection = true;
            }

            if (GUILayout.Button("Delete Node"))
            {
                if (EditorUtility.DisplayDialog("Delete State",
                        $"Are you sure you want to delete '{p_state.StateName}'?",
                        "Delete", "Cancel"))
                {
                    EditorApplication.delayCall += () => {
                        CleanReferencesToState(p_state);
                        currentStateMachineGui.DeleteState(p_state);
                        UpdateContentRect();
                        Repaint();
                    };
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.EndArea();
        }

        // Método auxiliar para crear texturas para los estilos
        private Texture2D MakeTex(int p_width, int p_height, Color p_col)
        {
            Color[] pix = new Color[p_width * p_height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = p_col;
            }
            Texture2D result = new Texture2D(p_width, p_height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private readonly Vector3 conditionBoxRect = new Vector3(150, 100);
        private void DrawConnection(GuiStateData p_from,int p_indexConnection, GuiStateData p_to)
        {
            Handles.BeginGUI();

            // Configurar estilo y color
            Handles.color = Color.yellow;
            var l_connectionColor = new Color(1f, 0.8f, 0f, 1f); // Amarillo más cálido
            var shadowColor = new Color(0f, 0f, 0f, 0.2f);

            // Calcular puntos de control para la curva Bezier
            Vector3 startPos = new Vector3(
                CONTROL_PANEL_WIDTH + p_from.Rect.xMax * zoomScale - scrollPosition.x,
                (p_from.Rect.y + p_from.Rect.height * 0.5f) * zoomScale - scrollPosition.y
            );

            Vector3 endPos = new Vector3(
                CONTROL_PANEL_WIDTH + p_to.Rect.xMin * zoomScale - scrollPosition.x,
                (p_to.Rect.y + p_to.Rect.height * 0.5f) * zoomScale - scrollPosition.y
            );

            // Calcular puntos de control para la curva
            float tangentLength = Mathf.Min(150f, Vector3.Distance(startPos, endPos) * 0.5f);
            Vector3 startTangent = startPos + Vector3.right * tangentLength;
            Vector3 endTangent = endPos + Vector3.left * tangentLength;

            // Dibujar sombra de la curva
            Handles.color = shadowColor;
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, shadowColor, null, 5f);

            // Dibujar curva principal
            Handles.color = l_connectionColor;
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, l_connectionColor, null, 3f);

            // Dibujar caja de condición en el punto medio de la curva
            Vector3 midPoint = BezierPoint(0.5f, startPos, endPos, startTangent, endTangent);

            Rect conditionRect = new Rect(
                midPoint.x - (conditionBoxRect.x * 0.5f * zoomScale),
                midPoint.y - (conditionBoxRect.y * 0.5f * zoomScale),
                conditionBoxRect.x * zoomScale,
                conditionBoxRect.y * zoomScale
            );

            // Resto del código para dibujar la caja de condición...
            GUI.Box(conditionRect, "", EditorStyles.helpBox);
            GUILayout.BeginArea(conditionRect);
            EditorGUILayout.LabelField("State Condition:", EditorStyles.boldLabel);

            p_from.StateCondition[p_indexConnection] = (StateCondition)EditorGUILayout.ObjectField(
                p_from.StateCondition[p_indexConnection],
                typeof(StateCondition),
                false);

            if (GUILayout.Button("Delete Connection"))
            {
                // Get the actual StateData objects before deletion
                StateData fromState = p_from.AssociatedStateData;
                StateCondition condition = p_from.StateCondition[p_indexConnection];
                StateData toState = p_to.AssociatedStateData;

                // Delete the connection
                currentStateMachineGui.DeleteConnection(fromState, condition, toState);
                UpdateContentRect();

                // Force immediate repaint to avoid drawing issues
                Repaint();

                // Break out of the current GUI cycle
                GUIUtility.ExitGUI();
            }

            GUILayout.EndArea();

            // Dibujar flecha al final de la curva
            Vector3 arrowDir = (BezierPoint(0.95f, startPos, endPos, startTangent, endTangent) -
                               BezierPoint(0.9f, startPos, endPos, startTangent, endTangent));
            arrowDir.Normalize();

            float arrowSize = 15f * zoomScale;
            Vector3 arrowTip = endPos;
            Vector3 arrowLeft = arrowTip + Quaternion.Euler(0, 0, 30) * -arrowDir * arrowSize;
            Vector3 arrowRight = arrowTip + Quaternion.Euler(0, 0, -30) * -arrowDir * arrowSize;

            Handles.DrawAAPolyLine(3f, arrowTip, arrowLeft);
            Handles.DrawAAPolyLine(3f, arrowTip, arrowRight);

            Handles.EndGUI();
        }

        private void CreateStateDataForNode(GuiStateData p_state)
        {
            StateData newStateData = ScriptableObject.CreateInstance<StateData>();
            newStateData.name = p_state.StateName;

            string path = AssetDatabase.GenerateUniqueAssetPath($"{customStateDataPath}/{p_state.StateName}.asset");
            AssetDatabase.CreateAsset(newStateData, path);
    
            p_state.AssociatedStateData = newStateData;
            currentStateMachineGui.AddState(newStateData, p_state.Rect);
    
            EditorUtility.SetDirty(targetStateMachine);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
    
            Repaint();
        }

        #region ControlEvents

        private void HandleNodeDragging(Event p_e)
        {

            // Convertir la posición del mouse a coordenadas del workspace considerando zoom y scroll
            Vector2 mousePositionInWorkspace = (p_e.mousePosition - new Vector2(CONTROL_PANEL_WIDTH, 0) + scrollPosition) / zoomScale;

            switch (p_e.type)
            {
                case EventType.MouseDown:
                    foreach (var guiState in currentStateMachineGui.AllGuiStates)
                    {
                        // Usar las coordenadas reales del nodo (sin escalar)
                        if (guiState.Rect.Contains(mousePositionInWorkspace))
                        {
                            if (isCreatingConnection && connectionStartNode != null)
                            {
                                if (guiState != connectionStartNode)
                                {
                                    connectionStartNode.AddGuiConnections(guiState, defaultStateCondition);
                                    isCreatingConnection = false;
                                    connectionStartNode = null;
                                    Repaint();
                                }
                                p_e.Use();
                                return;
                            }

                            isDragging = true;
                            dragStart = mousePositionInWorkspace - guiState.Rect.position;
                            guiState.IsBeingDragged = true;
                            p_e.Use();
                            break;
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        foreach (var guiState in currentStateMachineGui.AllGuiStates)
                        {
                            if (guiState.IsBeingDragged)
                            {
                                Vector2 newPosition = mousePositionInWorkspace - dragStart;

                                if (snapToGrid)
                                {
                                    newPosition.x = Mathf.Round(newPosition.x / gridSize) * gridSize;
                                    newPosition.y = Mathf.Round(newPosition.y / gridSize) * gridSize;
                                }

                                guiState.Rect.position = newPosition;
                                p_e.Use();
                                Repaint();
                                break;
                            }
                        }
                        UpdateContentRect();
                    }
                    break;

                case EventType.MouseUp:
                    isDragging = false;
                    foreach (var guiState in currentStateMachineGui.AllGuiStates)
                    {
                        guiState.IsBeingDragged = false;
                    }
                    UpdateContentRect();
                    Repaint();
                    break;

                case EventType.KeyDown:
                    if (p_e.keyCode == KeyCode.Delete)
                    {
                        // Eliminar nodo seleccionado
                        p_e.Use();
                    }
                    break;
            }
        }

        private void HandleZoom(Event p_e)
        {
            if (p_e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -p_e.delta.y * 0.05f;
                Vector2 mousePosition = p_e.mousePosition + scrollPosition;

                float prevZoom = zoomScale;
                zoomScale = Mathf.Clamp(zoomScale + zoomDelta, ZOOM_MIN, ZOOM_MAX);

                scrollPosition += (mousePosition - scrollPosition) * (1 - (zoomScale / prevZoom));

                p_e.Use();
                Repaint();
            }
        }


        #endregion
        private void CreateNewState()
        {
            // Crear nuevo nodo sin StateData asociado
            var newNodeRect = new Rect(
                50 + 300 * currentStateMachineGui.AllGuiStates.Count,
                100, 
                200, 
                190
            );

            var newGuiState = new GuiStateData(
                null, // No StateData inicial
                "New State", 
                newNodeRect,
                null, // No MyState inicial
                new List<StateCondition>(),
                new List<StateData>()
            );

            currentStateMachineGui.AddGuiState(newGuiState);
            UpdateContentRect();
            Repaint();
        }

        private void ValidateData()
        {
            if (currentStateMachineGui == null) return;

            // Validar estados sin nombre
            foreach (var state in currentStateMachineGui.AllGuiStates)
            {
                if (string.IsNullOrEmpty(state.StateName))
                {
                    Debug.LogWarning($"State has no name at position ({state.Rect.x}, {state.Rect.y})");
                }
            }

            // Validar condiciones nulas
            foreach (var state in currentStateMachineGui.AllGuiStates)
            {
                for (int i = 0; i < state.StateCondition.Count; i++)
                {
                    if (state.StateCondition[i] == null)
                    {
                        Debug.LogWarning($"Null condition found in state '{state.StateName}' for connection to '{state.GuiConnections[i].StateName}'");
                    }
                }
            }
        }

        // Llama a este método en SaveFsmChanges antes de guardar
        private bool CanSaveData()
        {
            if (currentStateMachineGui == null) return false;

            bool isValid = true;

            // Verificar nombres únicos
            var nameSet = new HashSet<string>();
            foreach (var state in currentStateMachineGui.AllGuiStates)
            {
                if (string.IsNullOrEmpty(state.StateName))
                {
                    Debug.LogError("All states must have a name!");
                    isValid = false;
                }
                else if (nameSet.Contains(state.StateName))
                {
                    Debug.LogError($"Duplicate state name: {state.StateName}");
                    isValid = false;
                }
                nameSet.Add(state.StateName);
            }

            return isValid;
        }

        [ContextMenu("ResetData")]
        [ContextMenu("ResetData")]
        private void ResetData()
        {
            if (targetStateMachine == null) return;

            // Registrar undo
            Undo.RecordObject(this, "Reset FSM Data");

            // Recargar datos desde el asset
            currentStateMachineGui = new GuiFsmData(targetStateMachine);
            CreateData();

            isCreatingConnection = false;
            connectionStartNode = null;
            UpdateContentRect();
            Repaint();
    
            Debug.Log("FSM data reset to saved state");
        }

        private void SaveFsmChanges()
        {
            if (targetStateMachine == null || currentStateMachineGui == null) 
                return;

            Undo.RecordObject(targetStateMachine, "Save FSM Changes");

            // Actualizar todos los estados
            targetStateMachine.ClearStates();
    
            foreach (var guiState in currentStateMachineGui.AllGuiStates)
            {
                if (guiState.AssociatedStateData != null)
                {
                    // Actualizar el nombre del StateData si cambió
                    if (guiState.AssociatedStateData.name != guiState.StateName)
                    {
                        guiState.AssociatedStateData.name = guiState.StateName;
                        EditorUtility.SetDirty(guiState.AssociatedStateData);
                    }

                    // Actualizar el estado
                    guiState.AssociatedStateData.SetData(
                        guiState.State,
                        guiState.StateCondition,
                        guiState.GuiConnections
                            .Where(c => c.AssociatedStateData != null)
                            .Select(c => c.AssociatedStateData)
                            .ToList()
                    );

                    targetStateMachine.AddState(guiState.AssociatedStateData);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(targetStateMachine);
    
            Debug.Log("FSM changes saved successfully");
        }

        

        private void CleanReferencesToState(GuiStateData p_stateToDelete)
        {
            foreach (var guiState in currentStateMachineGui.AllGuiStates)
            {
                // Eliminar conexiones que apuntan al estado que se va a borrar
                for (int i = guiState.GuiConnections.Count - 1; i >= 0; i--)
                {
                    if (guiState.GuiConnections[i] == p_stateToDelete)
                    {
                        guiState.GuiConnections.RemoveAt(i);
                        guiState.StateCondition.RemoveAt(i);
                        guiState.ExitStates.RemoveAt(i);
                    }
                }
            }
        }

        private void RenameStateAsset(GuiStateData stateData)
        {
            if (stateData.AssociatedStateData == null) return;

            string currentPath = AssetDatabase.GetAssetPath(stateData.AssociatedStateData);
            if (string.IsNullOrEmpty(currentPath)) return;

            // Validar nombre
            if (string.IsNullOrEmpty(stateData.StateName) || 
                stateData.StateName == stateData.AssociatedStateData.name)
            {
                return;
            }

            // Generar path único
            string directory = System.IO.Path.GetDirectoryName(currentPath);
            string extension = System.IO.Path.GetExtension(currentPath);
            string newPath = System.IO.Path.Combine(directory, stateData.StateName + extension);
            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

            // Renombrar el asset
            string error = AssetDatabase.RenameAsset(currentPath, System.IO.Path.GetFileNameWithoutExtension(newPath));
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("Error renaming state asset: " + error);
                return;
            }

            // Actualizar datos
            stateData.AssociatedStateData.name = stateData.StateName;
            EditorUtility.SetDirty(stateData.AssociatedStateData);
    
            // Forzar guardado
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }



        /// <summary>
        /// Calcula un punto en una curva Bezier cúbica
        /// </summary>
        /// <param name="p_t">Valor entre 0 y 1 que representa la posición en la curva</param>
        /// <param name="p_0">Punto inicial</param>
        /// <param name="p_1">Punto final</param>
        /// <param name="p_2">Primer punto de control (tangente inicial)</param>
        /// <param name="p_3">Segundo punto de control (tangente final)</param>
        /// <returns>Punto en la curva Bezier</returns>
        private static Vector3 BezierPoint(float p_t, Vector3 p_0, Vector3 p_1, Vector3 p_2, Vector3 p_3)
        {
            p_t = Mathf.Clamp01(p_t);
            float u = 1f - p_t;
            float tt = p_t * p_t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * p_t;

            Vector3 point = uuu * p_0;         // (1-t)³ * P0
            point += 3f * uu * p_t * p_2;       // 3(1-t)²t * P1
            point += 3f * u * tt * p_3;       // 3(1-t)t² * P2
            point += ttt * p_1;               // t³ * P3

            return point;
        }


    }
}
