using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Extensions.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "StateData", menuName = "Main/FSM/StateData", order = 0)]
    public class StateData : ScriptableObject
    {
        [field: SerializeField] public MyState MyState;

        [field: SerializeField] public List<StateCondition> StateConditions { get; private set; }
        [field: SerializeField] public List<StateData> ExitStates { get; private set; }

        private void Awake()
        {
            if (!StateConditions.IsUnityNull() && StateConditions.Count != ExitStates.Count)
                Debug.LogError($"ERROR EN {MyState.StateName}, ExitStates y Conditions no son congruentes");
        }

        public void SetData(MyState p_myState, List<StateCondition> p_stateConditions, List<StateData> p_exitStates)
        {
            MyState = p_myState;
            StateConditions = p_stateConditions;
            ExitStates = p_exitStates;
            
            // Actualizar el nombre si hay un estado asociado
            if (p_myState != null && this.name != p_myState.name)
            {
                this.name = p_myState.name;
            }
        }

        public void ChangeName(ScriptableObject p_scriptableObject, string p_name)
        {
            string assetPath = AssetDatabase.GetAssetPath(p_scriptableObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("El ScriptableObject no est� guardado como un asset en el proyecto.");
                return;
            }

            string result = AssetDatabase.RenameAsset(assetPath, p_name);

            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError($"Error al renombrar el ScriptableObject: {result}");
            }

            // Guarda los cambios y refresca el AssetDatabase
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public string GetPath()
        {
            // Obtiene la ruta completa del ScriptableObject
            string assetPath = AssetDatabase.GetAssetPath(this);

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("El ScriptableObject no se encuentra dentro de una carpeta de Assets.");
                return null;
            }

            // Extrae la ruta de la carpeta eliminando el nombre del archivo
            return System.IO.Path.GetDirectoryName(assetPath);
        }
    }
}