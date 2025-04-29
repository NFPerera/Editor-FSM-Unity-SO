using System.Collections.Generic;
using System.Linq;
using Extensions.FSM.Base.StateMachine;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Extensions.FSM.Editor
{
    public class GuiFsmData
    {
        public GuiFsmData(MyStateMachine p_targetFsm)
        {
            TargetFsm = p_targetFsm;
            AllStates = p_targetFsm.GetAllStates();
        }

        public MyStateMachine TargetFsm;
        public List<StateData> AllStates = new();
        public List<GuiStateData> AllGuiStates = new();

        public void AddGuiState(GuiStateData p_data) => AllGuiStates.Add(p_data);

        public void AddState(StateData p_data, Rect p_rect)
        {
            AllStates.Add(p_data);

            var l_guiData = new GuiStateData(p_data,
                p_data.name,
                p_rect,
                p_data.MyState,
                p_data.StateConditions,
                p_data.ExitStates);

            AddGuiState(l_guiData);
        }

        public void ClearData() => AllGuiStates.Clear();

        public void DeleteState(GuiStateData stateToDelete)
        {
            // Eliminar el estado de la lista principal
            AllGuiStates.Remove(stateToDelete);

            // Eliminar referencias en otros estados
            foreach (var state in AllGuiStates)
            {
                for (int i = state.GuiConnections.Count - 1; i >= 0; i--)
                {
                    if (state.GuiConnections[i] == stateToDelete)
                    {
                        state.GuiConnections.RemoveAt(i);
                        state.StateCondition.RemoveAt(i);
                        state.ExitStates.RemoveAt(i);
                    }
                }
            }

            // Eliminar el StateData asociado si existe
            if (stateToDelete.AssociatedStateData != null)
            {
                AllStates.Remove(stateToDelete.AssociatedStateData);
            }
        }

        public void DeleteConnection(StateData p_targetData,StateCondition p_condition, StateData p_exitState)
        {
            var guiFromState = AllGuiStates.FirstOrDefault(g => g.AssociatedStateData == p_targetData);
            var guiToState = AllGuiStates.FirstOrDefault(g => g.AssociatedStateData == p_exitState);

            if (guiFromState != null && guiToState != null)
            {
                // Find the index of the connection to remove
                int index = guiFromState.GuiConnections.FindIndex(g => g == guiToState);

                if (index >= 0)
                {
                    // Remove from all related collections
                    guiFromState.GuiConnections.RemoveAt(index);
                    guiFromState.StateCondition.RemoveAt(index);
                    guiFromState.ExitStates.RemoveAt(index);
                }
            }
        }

        public bool IsStateDataSaved(StateData p_stateData)
        {
            for (int i = 0; i < AllGuiStates.Count; i++)
            {
                if (AllGuiStates[i].AssociatedStateData == p_stateData)
                    return true;

            }
            return false;
        }
    }
}