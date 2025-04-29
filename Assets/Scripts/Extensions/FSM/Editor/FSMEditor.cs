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

        private string m_customStateDataPath = "Assets/";
        private bool m_showStateDataCreation = false;
        private const float CONTROL_PANEL_WIDTH = 300f;
        private const float CONTROL_PANEL_PADDING = 10f;
        private bool m_snapToGrid = true;
        private float m_gridSize = 20f;
        private string m_lastEditedStateName;
        private GuiStateData m_currentlyEditingState;

        #region ZoomControls

        private float m_zoomScale = 1.0f;
        private const float ZOOM_MIN = 0.5f; // Zoom mínimo
        private const float ZOOM_MAX = 2.0f; // Zoom máximo

        #endregion

        #region Scroll

        private Vector2 m_scrollPosition = Vector2.zero;
        private Rect m_contentRect = new Rect(0, 0, 2000, 2000); // Un área grande para moverse


        #endregion

        private bool m_isDragging;
        private Vector2 m_dragStart;

        private MyStateMachine m_targetStateMachine;
        private StateCondition m_defaultStateCondition;
        private GuiFsmData m_currentStateMachineGui;

        private Color m_connectionColor = Color.red;
        private Texture2D m_connectionTexture;

        void OnEnable()
        {
            m_connectionTexture = new Texture2D(1, 1);
            m_connectionTexture.SetPixel(0, 0, Color.white);
            m_connectionTexture.Apply();

            // Modifica el dibujo de conexión temporal
            if (m_isCreatingConnection && m_connectionStartNode != null)
            {
                Handles.BeginGUI();

                // Cambiar color basado en validez
                bool l_isValidTarget = false;
                if (Event.current.mousePosition != null)
                {
                    foreach (var l_state in m_currentStateMachineGui.AllGuiStates)
                    {
                        Rect l_scaledRect = new Rect(
                            l_state.Rect.x * m_zoomScale - m_scrollPosition.x,
                            l_state.Rect.y * m_zoomScale - m_scrollPosition.y,
                            l_state.Rect.width * m_zoomScale,
                            l_state.Rect.height * m_zoomScale
                        );

                        if (l_scaledRect.Contains(Event.current.mousePosition))
                        {
                            l_isValidTarget = l_state != m_connectionStartNode;
                            break;
                        }
                    }
                }

                m_connectionColor = l_isValidTarget ? Color.green : Color.red;
                Handles.color = m_connectionColor;

                Vector3 l_start = new Vector3(m_connectionStartNode.Rect.xMax * m_zoomScale,
                    (m_connectionStartNode.Rect.y + m_connectionStartNode.Rect.height / 2) * m_zoomScale);
                Vector3 l_end = Event.current.mousePosition + m_scrollPosition;

                // Línea más gruesa con patrón
                Handles.DrawAAPolyLine(5f, l_start, l_end);

                // Dibujar flecha
                Vector3 l_direction = (l_end - l_start).normalized;
                float l_arrowSize = 15f;
                Vector3 l_right = Quaternion.Euler(0, 0, 30) * -l_direction * l_arrowSize;
                Vector3 l_left = Quaternion.Euler(0, 0, -30) * -l_direction * l_arrowSize;

                Handles.DrawAAPolyLine(3f, l_end, l_end + l_right);
                Handles.DrawAAPolyLine(3f, l_end, l_end + l_left);

                Handles.EndGUI();
            }
        }

        private void OnDestroy()
        {
            // Guardar automáticamente al cerrar la ventana
            if (m_targetStateMachine != null && m_currentStateMachineGui != null)
            {
                SaveFsmChanges();
            }
        }

        private void OnGUI()
        {
            // Dividir el espacio en dos áreas
            Rect l_controlPanelRect = new Rect(0, 0, CONTROL_PANEL_WIDTH, position.height);
            Rect l_workspaceRect =
                new Rect(CONTROL_PANEL_WIDTH, 0, position.width - CONTROL_PANEL_WIDTH, position.height);

            // Dibujar el panel de control
            DrawControlPanel(l_controlPanelRect);

            // Dibujar el área de trabajo
            DrawWorkspace(l_workspaceRect);

            UpdateContentRect();

            // Forzar repintado continuo durante la creación de conexión
            if (m_isCreatingConnection)
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
                GenericMenu l_menu = new GenericMenu();
                l_menu.AddItem(new GUIContent("Reset FSM Data"), false, ResetData);
                l_menu.ShowAsContext();
            }

            m_targetStateMachine = (MyStateMachine)EditorGUILayout.ObjectField("State Machine", m_targetStateMachine,
                typeof(MyStateMachine), false);

            m_defaultStateCondition = (StateCondition)EditorGUILayout.ObjectField("Default Condition",
                m_defaultStateCondition, typeof(StateCondition), false);

            if (m_targetStateMachine == null)
            {
                EditorGUILayout.HelpBox("Please assign a State Machine to edit.", MessageType.Info);
            }
            else if (m_defaultStateCondition == null)
            {
                EditorGUILayout.HelpBox("Please assign a Default State condition.", MessageType.Info);
            }

            GUILayout.Space(10);



            m_snapToGrid = EditorGUILayout.Toggle("Snap to Grid", m_snapToGrid);
            if (m_snapToGrid)
            {
                m_gridSize = EditorGUILayout.FloatField("Grid Size", m_gridSize);
            }

            // Sección para la ruta personalizada
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("StateData Storage", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            m_customStateDataPath = EditorGUILayout.TextField("Path", m_customStateDataPath);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                string l_selectedPath =
                    EditorUtility.SaveFolderPanel("Select StateData Folder", m_customStateDataPath, "");
                if (!string.IsNullOrEmpty(l_selectedPath))
                {
                    // Convertir a ruta relativa de Assets
                    if (l_selectedPath.StartsWith(Application.dataPath))
                    {
                        m_customStateDataPath = "Assets" + l_selectedPath.Substring(Application.dataPath.Length);
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
            Rect l_adjustedContentRect = new Rect(
                m_contentRect.x,
                m_contentRect.y,
                m_contentRect.width + CONTROL_PANEL_WIDTH,
                m_contentRect.height
            );

            // Ajustar el scroll position para el área de trabajo
            Vector2 l_adjustedScrollPosition = GUI.BeginScrollView(
                p_rect,
                m_scrollPosition,
                l_adjustedContentRect,
                true,
                true
            );

            // Guardar la matriz actual para aplicar transformaciones
            Matrix4x4 l_originalMatrix = GUI.matrix;

            try
            {
                // Aplicar zoom y desplazamiento
                GUIUtility.ScaleAroundPivot(Vector2.one * m_zoomScale, Vector2.zero);
                GUI.matrix = Matrix4x4.TRS(
                    new Vector3(-l_adjustedScrollPosition.x, -l_adjustedScrollPosition.y, 0),
                    Quaternion.identity,
                    Vector3.one
                );

                // Dibujar los nodos y conexiones
                if (m_targetStateMachine != null && m_defaultStateCondition != null)
                {
                    if (m_currentStateMachineGui == null || m_targetStateMachine != m_currentStateMachineGui.TargetFsm)
                    {
                        m_currentStateMachineGui = new GuiFsmData(m_targetStateMachine);
                        CreateData();
                    }

                    DrawNodeThree();
                    HandleZoom(Event.current);
                    HandleNodeDragging(Event.current);

                    if (m_isCreatingConnection && m_connectionStartNode != null)
                    {
                        DrawTemporaryConnection();
                    }
                }
            }
            finally
            {
                // Restaurar la matriz original
                GUI.matrix = l_originalMatrix;
                GUI.EndScrollView();
            }
        }

        private void DrawTemporaryConnection()
        {
            if (!m_isCreatingConnection || m_connectionStartNode == null) return;


            Handles.BeginGUI();

            Handles.color = Color.green;
            Vector2 l_mousePos = Event.current.mousePosition;

            // Calcular posición de inicio (centro derecho del nodo inicial)
            Vector3 l_startPos = new Vector3(
                CONTROL_PANEL_WIDTH + m_connectionStartNode.Rect.xMax * m_zoomScale - m_scrollPosition.x,
                (m_connectionStartNode.Rect.y + m_connectionStartNode.Rect.height * 0.5f) * m_zoomScale -
                m_scrollPosition.y
            );

            // Verificar si el mouse está sobre otro nodo
            bool l_isValidTarget = false;
            foreach (var l_state in m_currentStateMachineGui.AllGuiStates)
            {
                Rect l_scaledRect = new Rect(
                    CONTROL_PANEL_WIDTH + l_state.Rect.x * m_zoomScale - m_scrollPosition.x,
                    l_state.Rect.y * m_zoomScale - m_scrollPosition.y,
                    l_state.Rect.width * m_zoomScale,
                    l_state.Rect.height * m_zoomScale
                );

                if (l_scaledRect.Contains(l_mousePos))
                {
                    l_isValidTarget = l_state != m_connectionStartNode;
                    break;
                }
            }

            // Cambiar color según validez
            Handles.color = l_isValidTarget ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);

            // Calcular puntos para la curva Bezier
            Vector3 l_endPos = l_mousePos;
            float l_tangentLength = Mathf.Min(150f, Vector3.Distance(l_startPos, l_endPos)) * 0.5f;
            Vector3 l_startTangent = l_startPos + Vector3.right * l_tangentLength;
            Vector3 l_endTangent = l_endPos + Vector3.left * l_tangentLength;

            // Dibujar curva temporal más suave
            Handles.DrawBezier(l_startPos, l_endPos, l_startTangent, l_endTangent, Handles.color, null, 3f);

            // Dibujar flecha solo si es un objetivo válido
            if (l_isValidTarget)
            {
                Vector3 l_arrowDir = (BezierPoint(0.95f, l_startPos, l_endPos, l_startTangent, l_endTangent) -
                                      BezierPoint(0.9f, l_startPos, l_endPos, l_startTangent, l_endTangent));
                l_arrowDir.Normalize();

                float l_arrowSize = 12f;
                Vector3 l_arrowTip = l_endPos;
                Vector3 l_arrowLeft = l_arrowTip + Quaternion.Euler(0, 0, 30) * -l_arrowDir * l_arrowSize;
                Vector3 l_arrowRight = l_arrowTip + Quaternion.Euler(0, 0, -30) * -l_arrowDir * l_arrowSize;

                Handles.DrawAAPolyLine(3f, l_arrowTip, l_arrowLeft);
                Handles.DrawAAPolyLine(3f, l_arrowTip, l_arrowRight);
            }

            Handles.EndGUI();
        }

        // Modifica UpdateContentRect para tener en cuenta el panel de control
        void UpdateContentRect()
        {
            if (m_currentStateMachineGui == null || m_currentStateMachineGui.AllGuiStates == null ||
                m_currentStateMachineGui.AllGuiStates.Count == 0)
            {
                // Tamaño por defecto cuando no hay nodos
                m_contentRect = new Rect(CONTROL_PANEL_WIDTH, 0, position.width, position.height);
                return;
            }

            float l_minX = float.MaxValue, l_minY = float.MaxValue;
            float l_maxX = float.MinValue, l_maxY = float.MinValue;

            foreach (var l_state in m_currentStateMachineGui.AllGuiStates)
            {
                l_minX = Mathf.Min(l_minX, l_state.Rect.x);
                l_minY = Mathf.Min(l_minY, l_state.Rect.y);
                l_maxX = Mathf.Max(l_maxX, l_state.Rect.x + l_state.Rect.width);
                l_maxY = Mathf.Max(l_maxY, l_state.Rect.y + l_state.Rect.height);
            }

            // Añadir márgenes (100px alrededor del contenido)
            float l_margin = 100f;
            m_contentRect = new Rect(
                CONTROL_PANEL_WIDTH,
                0,
                Mathf.Max(l_maxX - l_minX + 2 * l_margin, position.width - CONTROL_PANEL_WIDTH),
                Mathf.Max(l_maxY - l_minY + 2 * l_margin, position.height)
            );
        }

        private void DrawNodeThree()
        {
            // Crear copias de las listas para iterar
            var l_statesToDraw = new List<GuiStateData>(m_currentStateMachineGui.AllGuiStates);
            var l_connectionsToDraw = new List<Tuple<GuiStateData, GuiStateData>>();

            // Primero recolectar todas las conexiones
            foreach (var l_guiState in l_statesToDraw)
            {
                for (int l_j = 0; l_j < l_guiState.GuiConnections.Count; l_j++)
                {
                    l_connectionsToDraw.Add(
                        new Tuple<GuiStateData, GuiStateData>(l_guiState, l_guiState.GuiConnections[l_j]));
                }
            }

            // Calcular área visible
            Rect l_visibleArea = new Rect(
                m_scrollPosition.x / m_zoomScale,
                m_scrollPosition.y / m_zoomScale,
                position.width / m_zoomScale,
                position.height / m_zoomScale
            );

            // Dibujar conexiones
            foreach (var l_connection in l_connectionsToDraw)
            {
                var l_from = l_connection.Item1;
                var l_to = l_connection.Item2;

                if (l_from.Rect.Overlaps(l_visibleArea) || l_to.Rect.Overlaps(l_visibleArea))
                {
                    int l_index = l_from.GuiConnections.IndexOf(l_to);
                    if (l_index >= 0)
                    {
                        DrawConnection(l_from, l_index, l_to);
                    }
                }
            }

            // Dibujar nodos
            foreach (var l_guiState in l_statesToDraw)
            {
                if (l_guiState.Rect.Overlaps(l_visibleArea))
                {
                    DrawNode(l_guiState);
                }
            }
        }

        private void CreateData()
        {
            // Limpiar datos existentes
            m_currentStateMachineGui.AllGuiStates.Clear();

            // Crear nodos para los estados existentes
            for (int l_i = 0; l_i < m_currentStateMachineGui.AllStates.Count; l_i++)
            {
                var l_stateData = m_currentStateMachineGui.AllStates[l_i];
                Rect l_nodeRect = new Rect(50 + 300 * l_i, 100, 200, 190);

                var l_guiData = new GuiStateData(
                    l_stateData,
                    l_stateData.name,
                    l_nodeRect,
                    l_stateData.MyState,
                    l_stateData.StateConditions,
                    l_stateData.ExitStates
                );

                m_currentStateMachineGui.AddGuiState(l_guiData);
            }

            // Actualizar conexiones
            foreach (var l_guiState in m_currentStateMachineGui.AllGuiStates)
            {
                var l_connections = new List<GuiStateData>();
                foreach (var l_exitState in l_guiState.ExitStates)
                {
                    var l_connectedGuiState = m_currentStateMachineGui.AllGuiStates
                        .FirstOrDefault(p_g => p_g.AssociatedStateData == l_exitState);
                    if (l_connectedGuiState != null)
                    {
                        l_connections.Add(l_connectedGuiState);
                    }
                }

                l_guiState.SetGuiConnections(l_connections);
            }

            UpdateContentRect();
        }

        private GuiStateData m_connectionStartNode;
        private bool m_isCreatingConnection;

        private void DrawNode(GuiStateData p_state)
        {
            Rect l_scaledRect = new Rect(
                CONTROL_PANEL_WIDTH + p_state.Rect.x * m_zoomScale - m_scrollPosition.x,
                p_state.Rect.y * m_zoomScale - m_scrollPosition.y,
                p_state.Rect.width * m_zoomScale,
                p_state.Rect.height * m_zoomScale
            );

            // Estilo del nodo basado en si está siendo editado
            bool l_isBeingEdited = GUI.GetNameOfFocusedControl() == "StateNameField_" + p_state.UniqueId;
            GUIStyle l_boxStyle = l_isBeingEdited
                ? new GUIStyle(EditorStyles.helpBox)
                    { normal = { background = MakeTex(2, 2, new Color(0.1f, 0.3f, 0.5f, 0.2f)) } }
                : EditorStyles.helpBox;

            GUI.Box(l_scaledRect, "", l_boxStyle);
            GUILayout.BeginArea(l_scaledRect);

            // --- SECCIÓN DE NOMBRE DEL ESTADO ---
            EditorGUILayout.LabelField("State Name:", EditorStyles.boldLabel);

            string l_previousName = p_state.StateName;
            GUI.SetNextControlName("StateNameField_" + p_state.UniqueId);

            // Guardar el nombre antes de la edición
            string l_nameBeforeEdit = p_state.StateName;

            // Mostrar campo de texto
            p_state.StateName = EditorGUILayout.TextField(p_state.StateName,
                new GUIStyle(EditorStyles.textField)
                    { normal = { textColor = l_isBeingEdited ? Color.cyan : Color.white } });

            // Manejo de eventos de teclado
            if (Event.current.isKey && GUI.GetNameOfFocusedControl() == "StateNameField_" + p_state.UniqueId)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    // Confirmar cambios
                    if (!string.IsNullOrEmpty(p_state.StateName))
                    {
                        if (p_state.AssociatedStateData != null && p_state.StateName != l_previousName)
                        {
                            RenameStateAsset(p_state);
                        }

                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                    else
                    {
                        p_state.StateName = l_previousName;
                    }
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    // Cancelar cambios
                    p_state.StateName = l_nameBeforeEdit;
                    m_currentlyEditingState = null;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            // Actualizar estado de edición
            if (GUI.GetNameOfFocusedControl() == "StateNameField_" + p_state.UniqueId)
            {
                if (m_currentlyEditingState != p_state)
                {
                    m_currentlyEditingState = p_state;
                }
            }
            else if (m_currentlyEditingState == p_state)
            {
                m_currentlyEditingState = null;
            }

            // --- SECCIÓN DE STATE DATA ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State Data", EditorStyles.boldLabel);

            var l_previousStateData = p_state.AssociatedStateData;
            p_state.AssociatedStateData = (StateData)EditorGUILayout.ObjectField(
                p_state.AssociatedStateData,
                typeof(StateData),
                false);

            // Botón para crear StateData si no existe
            if (p_state.AssociatedStateData == null)
            {
                if (GUILayout.Button("Create StateData"))
                {
                    CreateStateDataForNode(p_state);
                }
            }
            else if (l_previousStateData != p_state.AssociatedStateData)
            {
                // Actualizar nombre cuando cambia el StateData
                if (!l_isBeingEdited) // Solo si no estamos editando manualmente
                {
                    p_state.StateName = p_state.AssociatedStateData.name;
                }
            }

            // Mostrar comportamiento solo si hay StateData
            if (p_state.AssociatedStateData != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("State Behavior", EditorStyles.boldLabel);

                var l_previousState = p_state.State;
                p_state.State = (MyState)EditorGUILayout.ObjectField(
                    p_state.State,
                    typeof(MyState),
                    false);

                // Actualizar referencia si cambió
                if (l_previousState != p_state.State)
                {
                    p_state.AssociatedStateData.MyState = p_state.State;
                    EditorUtility.SetDirty(p_state.AssociatedStateData);
                }
            }

            // --- BOTONES DE ACCIÓN ---
            EditorGUILayout.Space();
            if (GUILayout.Button("Add Connection"))
            {
                m_connectionStartNode = p_state;
                m_isCreatingConnection = true;
            }

            if (GUILayout.Button("Delete Node"))
            {
                if (EditorUtility.DisplayDialog("Delete State",
                        $"Are you sure you want to delete '{p_state.StateName}'?",
                        "Delete", "Cancel"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        CleanReferencesToState(p_state);
                        m_currentStateMachineGui.DeleteState(p_state);
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
            Color[] l_pix = new Color[p_width * p_height];
            for (int l_i = 0; l_i < l_pix.Length; l_i++)
            {
                l_pix[l_i] = p_col;
            }
            Texture2D l_result = new Texture2D(p_width, p_height);
            l_result.SetPixels(l_pix);
            l_result.Apply();
            return l_result;
        }

        private readonly Vector3 m_conditionBoxRect = new Vector3(150, 100);
        private void DrawConnection(GuiStateData p_from,int p_indexConnection, GuiStateData p_to)
        {
            Handles.BeginGUI();

            // Configurar estilo y color
            Handles.color = Color.yellow;
            var l_connectionColor = new Color(1f, 0.8f, 0f, 1f); // Amarillo más cálido
            var l_shadowColor = new Color(0f, 0f, 0f, 0.2f);

            // Calcular puntos de control para la curva Bezier
            Vector3 l_startPos = new Vector3(
                CONTROL_PANEL_WIDTH + p_from.Rect.xMax * m_zoomScale - m_scrollPosition.x,
                (p_from.Rect.y + p_from.Rect.height * 0.5f) * m_zoomScale - m_scrollPosition.y
            );

            Vector3 l_endPos = new Vector3(
                CONTROL_PANEL_WIDTH + p_to.Rect.xMin * m_zoomScale - m_scrollPosition.x,
                (p_to.Rect.y + p_to.Rect.height * 0.5f) * m_zoomScale - m_scrollPosition.y
            );

            // Calcular puntos de control para la curva
            float l_tangentLength = Mathf.Min(150f, Vector3.Distance(l_startPos, l_endPos) * 0.5f);
            Vector3 l_startTangent = l_startPos + Vector3.right * l_tangentLength;
            Vector3 l_endTangent = l_endPos + Vector3.left * l_tangentLength;

            // Dibujar sombra de la curva
            Handles.color = l_shadowColor;
            Handles.DrawBezier(l_startPos, l_endPos, l_startTangent, l_endTangent, l_shadowColor, null, 5f);

            // Dibujar curva principal
            Handles.color = l_connectionColor;
            Handles.DrawBezier(l_startPos, l_endPos, l_startTangent, l_endTangent, l_connectionColor, null, 3f);

            // Dibujar caja de condición en el punto medio de la curva
            Vector3 l_midPoint = BezierPoint(0.5f, l_startPos, l_endPos, l_startTangent, l_endTangent);

            Rect l_conditionRect = new Rect(
                l_midPoint.x - (m_conditionBoxRect.x * 0.5f * m_zoomScale),
                l_midPoint.y - (m_conditionBoxRect.y * 0.5f * m_zoomScale),
                m_conditionBoxRect.x * m_zoomScale,
                m_conditionBoxRect.y * m_zoomScale
            );

            // Resto del código para dibujar la caja de condición...
            GUI.Box(l_conditionRect, "", EditorStyles.helpBox);
            GUILayout.BeginArea(l_conditionRect);
            EditorGUILayout.LabelField("State Condition:", EditorStyles.boldLabel);

            p_from.StateCondition[p_indexConnection] = (StateCondition)EditorGUILayout.ObjectField(
                p_from.StateCondition[p_indexConnection],
                typeof(StateCondition),
                false);

            if (GUILayout.Button("Delete Connection"))
            {
                // Get the actual StateData objects before deletion
                StateData l_fromState = p_from.AssociatedStateData;
                StateCondition l_condition = p_from.StateCondition[p_indexConnection];
                StateData l_toState = p_to.AssociatedStateData;

                // Delete the connection
                m_currentStateMachineGui.DeleteConnection(l_fromState, l_condition, l_toState);
                UpdateContentRect();

                // Force immediate repaint to avoid drawing issues
                Repaint();

                // Break out of the current GUI cycle
                GUIUtility.ExitGUI();
            }

            GUILayout.EndArea();

            // Dibujar flecha al final de la curva
            Vector3 l_arrowDir = (BezierPoint(0.95f, l_startPos, l_endPos, l_startTangent, l_endTangent) -
                               BezierPoint(0.9f, l_startPos, l_endPos, l_startTangent, l_endTangent));
            l_arrowDir.Normalize();

            float l_arrowSize = 15f * m_zoomScale;
            Vector3 l_arrowTip = l_endPos;
            Vector3 l_arrowLeft = l_arrowTip + Quaternion.Euler(0, 0, 30) * -l_arrowDir * l_arrowSize;
            Vector3 l_arrowRight = l_arrowTip + Quaternion.Euler(0, 0, -30) * -l_arrowDir * l_arrowSize;

            Handles.DrawAAPolyLine(3f, l_arrowTip, l_arrowLeft);
            Handles.DrawAAPolyLine(3f, l_arrowTip, l_arrowRight);

            Handles.EndGUI();
        }

        private void CreateStateDataForNode(GuiStateData p_state)
        {
            if (string.IsNullOrEmpty(p_state.StateName))
            {
                p_state.StateName = "NewState_" + Guid.NewGuid().ToString("N").Substring(0, 4);
            }

            StateData l_newStateData = ScriptableObject.CreateInstance<StateData>();
            l_newStateData.name = p_state.StateName;

            string l_path = AssetDatabase.GenerateUniqueAssetPath($"{m_customStateDataPath}/{p_state.StateName}.asset");
            AssetDatabase.CreateAsset(l_newStateData, l_path);
    
            p_state.AssociatedStateData = l_newStateData;
            EditorUtility.SetDirty(l_newStateData);
    
            // Si hay un estado asignado, guardar la referencia
            if (p_state.State != null)
            {
                l_newStateData.MyState = p_state.State;
            }
    
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Repaint();
        }

        #region ControlEvents

        private void HandleNodeDragging(Event p_e)
        {

            // Convertir la posición del mouse a coordenadas del workspace considerando zoom y scroll
            Vector2 l_mousePositionInWorkspace = (p_e.mousePosition - new Vector2(CONTROL_PANEL_WIDTH, 0) + m_scrollPosition) / m_zoomScale;

            switch (p_e.type)
            {
                case EventType.MouseDown:
                    foreach (var l_guiState in m_currentStateMachineGui.AllGuiStates)
                    {
                        // Usar las coordenadas reales del nodo (sin escalar)
                        if (l_guiState.Rect.Contains(l_mousePositionInWorkspace))
                        {
                            if (m_isCreatingConnection && m_connectionStartNode != null)
                            {
                                if (l_guiState != m_connectionStartNode)
                                {
                                    m_connectionStartNode.AddGuiConnections(l_guiState, m_defaultStateCondition);
                                    m_isCreatingConnection = false;
                                    m_connectionStartNode = null;
                                    Repaint();
                                }
                                p_e.Use();
                                return;
                            }

                            m_isDragging = true;
                            m_dragStart = l_mousePositionInWorkspace - l_guiState.Rect.position;
                            l_guiState.IsBeingDragged = true;
                            p_e.Use();
                            break;
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (m_isDragging)
                    {
                        foreach (var l_guiState in m_currentStateMachineGui.AllGuiStates)
                        {
                            if (l_guiState.IsBeingDragged)
                            {
                                Vector2 l_newPosition = l_mousePositionInWorkspace - m_dragStart;

                                if (m_snapToGrid)
                                {
                                    l_newPosition.x = Mathf.Round(l_newPosition.x / m_gridSize) * m_gridSize;
                                    l_newPosition.y = Mathf.Round(l_newPosition.y / m_gridSize) * m_gridSize;
                                }

                                l_guiState.Rect.position = l_newPosition;
                                p_e.Use();
                                Repaint();
                                break;
                            }
                        }
                        UpdateContentRect();
                    }
                    break;

                case EventType.MouseUp:
                    m_isDragging = false;
                    foreach (var l_guiState in m_currentStateMachineGui.AllGuiStates)
                    {
                        l_guiState.IsBeingDragged = false;
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
                float l_zoomDelta = -p_e.delta.y * 0.05f;
                Vector2 l_mousePosition = p_e.mousePosition + m_scrollPosition;

                float l_prevZoom = m_zoomScale;
                m_zoomScale = Mathf.Clamp(m_zoomScale + l_zoomDelta, ZOOM_MIN, ZOOM_MAX);

                m_scrollPosition += (l_mousePosition - m_scrollPosition) * (1 - (m_zoomScale / l_prevZoom));

                p_e.Use();
                Repaint();
            }
        }


        #endregion
        private void CreateNewState()
        {
            // Crear nuevo nodo sin StateData asociado
            var l_newNodeRect = new Rect(
                50 + 300 * m_currentStateMachineGui.AllGuiStates.Count,
                100, 
                200, 
                190
            );

            var l_newGuiState = new GuiStateData(
                null, // No StateData inicial
                "New State", 
                l_newNodeRect,
                null, // No MyState inicial
                new List<StateCondition>(),
                new List<StateData>()
            );

            m_currentStateMachineGui.AddGuiState(l_newGuiState);
            UpdateContentRect();
            Repaint();
        }

        private void ValidateData()
        {
            if (m_currentStateMachineGui == null) return;

            // Validar estados sin nombre
            foreach (var l_state in m_currentStateMachineGui.AllGuiStates)
            {
                if (string.IsNullOrEmpty(l_state.StateName))
                {
                    Debug.LogWarning($"State has no name at position ({l_state.Rect.x}, {l_state.Rect.y})");
                }
            }

            // Validar condiciones nulas
            foreach (var l_state in m_currentStateMachineGui.AllGuiStates)
            {
                for (int l_i = 0; l_i < l_state.StateCondition.Count; l_i++)
                {
                    if (l_state.StateCondition[l_i] == null)
                    {
                        Debug.LogWarning($"Null condition found in state '{l_state.StateName}' for connection to '{l_state.GuiConnections[l_i].StateName}'");
                    }
                }
            }
        }

        // Llama a este método en SaveFsmChanges antes de guardar
        private bool CanSaveData()
        {
            if (m_currentStateMachineGui == null) return false;

            bool l_isValid = true;

            // Verificar nombres únicos
            var l_nameSet = new HashSet<string>();
            foreach (var l_state in m_currentStateMachineGui.AllGuiStates)
            {
                if (string.IsNullOrEmpty(l_state.StateName))
                {
                    Debug.LogError("All states must have a name!");
                    l_isValid = false;
                }
                else if (l_nameSet.Contains(l_state.StateName))
                {
                    Debug.LogError($"Duplicate state name: {l_state.StateName}");
                    l_isValid = false;
                }
                l_nameSet.Add(l_state.StateName);
            }

            return l_isValid;
        }

        [ContextMenu("ResetData")]
        [ContextMenu("ResetData")]
        private void ResetData()
        {
            if (m_targetStateMachine == null) return;

            // Registrar undo
            Undo.RecordObject(this, "Reset FSM Data");

            // Recargar datos desde el asset
            m_currentStateMachineGui = new GuiFsmData(m_targetStateMachine);
            CreateData();

            m_isCreatingConnection = false;
            m_connectionStartNode = null;
            UpdateContentRect();
            Repaint();
    
            Debug.Log("FSM data reset to saved state");
        }

        private void SaveFsmChanges()
        {
            if (m_targetStateMachine == null || m_currentStateMachineGui == null) 
                return;

            Undo.RecordObject(m_targetStateMachine, "Save FSM Changes");

            // Actualizar todos los estados
            m_targetStateMachine.ClearStates();
    
            foreach (var l_guiState in m_currentStateMachineGui.AllGuiStates)
            {
                if (l_guiState.AssociatedStateData != null)
                {
                    // Actualizar el nombre del StateData si cambió
                    if (l_guiState.AssociatedStateData.name != l_guiState.StateName)
                    {
                        l_guiState.AssociatedStateData.name = l_guiState.StateName;
                        EditorUtility.SetDirty(l_guiState.AssociatedStateData);
                    }

                    // Actualizar el estado
                    l_guiState.AssociatedStateData.SetData(
                        l_guiState.State,
                        l_guiState.StateCondition,
                        l_guiState.GuiConnections
                            .Where(p_c => p_c.AssociatedStateData != null)
                            .Select(p_c => p_c.AssociatedStateData)
                            .ToList()
                    );

                    m_targetStateMachine.AddState(l_guiState.AssociatedStateData);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(m_targetStateMachine);
    
            Debug.Log("FSM changes saved successfully");
        }

        

        private void CleanReferencesToState(GuiStateData p_stateToDelete)
        {
            foreach (var l_guiState in m_currentStateMachineGui.AllGuiStates)
            {
                // Eliminar conexiones que apuntan al estado que se va a borrar
                for (int l_i = l_guiState.GuiConnections.Count - 1; l_i >= 0; l_i--)
                {
                    if (l_guiState.GuiConnections[l_i] == p_stateToDelete)
                    {
                        l_guiState.GuiConnections.RemoveAt(l_i);
                        l_guiState.StateCondition.RemoveAt(l_i);
                        l_guiState.ExitStates.RemoveAt(l_i);
                    }
                }
            }
        }

        private void RenameStateAsset(GuiStateData p_stateData)
        {
            if (p_stateData.AssociatedStateData == null || 
                string.IsNullOrEmpty(p_stateData.StateName) ||
                p_stateData.StateName == p_stateData.AssociatedStateData.name)
            {
                return;
            }

            string l_currentPath = AssetDatabase.GetAssetPath(p_stateData.AssociatedStateData);
            if (string.IsNullOrEmpty(l_currentPath)) return;

            string l_error = AssetDatabase.RenameAsset(l_currentPath, p_stateData.StateName);
            if (string.IsNullOrEmpty(l_error))
            {
                p_stateData.AssociatedStateData.name = p_stateData.StateName;
                EditorUtility.SetDirty(p_stateData.AssociatedStateData);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError($"Failed to rename state asset: {l_error}");
                p_stateData.StateName = p_stateData.AssociatedStateData.name; // Revertir cambio
            }
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
            float l_u = 1f - p_t;
            float l_tt = p_t * p_t;
            float l_uu = l_u * l_u;
            float l_uuu = l_uu * l_u;
            float l_ttt = l_tt * p_t;

            Vector3 l_point = l_uuu * p_0;         // (1-t)³ * P0
            l_point += 3f * l_uu * p_t * p_2;       // 3(1-t)²t * P1
            l_point += 3f * l_u * l_tt * p_3;       // 3(1-t)t² * P2
            l_point += l_ttt * p_1;               // t³ * P3

            return l_point;
        }


    }
}
