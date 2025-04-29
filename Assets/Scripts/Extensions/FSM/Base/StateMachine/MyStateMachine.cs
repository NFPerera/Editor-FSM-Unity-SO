using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.FSM.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Extensions.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "MyStateMachine", menuName = "Main/FSM/MyStateMachine", order = 0)]
    public sealed class MyStateMachine : ScriptableObject
    {
        [field: SerializeField] private List<StateData> allStatesData;
        private StateData m_currentState;
        private int m_currentStateConditionsAmount;

        private readonly Dictionary<Type, StateData> m_statesDictionary = new();
        private IUseFsm m_model;


        public void InitializeStateMachine(IUseFsm p_model)
        {
            m_model = p_model;

            Assert.IsNotNull(allStatesData);

            InitializedStatesCheck(allStatesData);
            InitializeStates(allStatesData);

            m_currentState = allStatesData[0];
            m_currentState.MyState.EnterState(m_model);
            m_currentStateConditionsAmount = m_currentState.StateConditions.Count;
        }

        private void InitializeStates(IReadOnlyList<StateData> p_enemyStatesData)
        {
            var l_statesCount = p_enemyStatesData.Count;

            for (var l_i = 0; l_i < l_statesCount; l_i++)
            {
                var l_type = p_enemyStatesData[l_i].MyState.GetType();

                if (m_statesDictionary.ContainsKey(l_type))
                    continue;

                m_statesDictionary.Add(l_type, p_enemyStatesData[l_i]);
            }
        }

        #region InitializationCheck

        private static void InitializedStatesCheck(IReadOnlyList<StateData> p_enemyStatesData)
        {
            if (p_enemyStatesData.Count < 1)
            {
                throw new Exception($"FSM {p_enemyStatesData} has no states assigned");
            }

            for (var l_i = 0; l_i < p_enemyStatesData.Count; l_i++)
            {
                var l_currState = p_enemyStatesData[l_i];

                if (l_currState == null)
                {
                    throw new Exception($"State in position {l_i} is null");
                }

                if (l_currState.ExitStates.Count != l_currState.StateConditions.Count)
                {
                    throw new Exception($"State {l_currState} doesn't have the same amount of exits and conditions");
                }

                if (l_currState.ExitStates.Any(p_exitState => p_exitState == null))
                {
                    throw new Exception($"State {l_currState} has an invalid exit state");
                }

                if (l_currState.StateConditions.Any(p_condition => p_condition == null))
                {
                    throw new Exception($"State {l_currState} has an invalid exit condition");
                }
            }
        }

        #endregion InitializationCheck

        public void RunStateMachine()
        {
            if (m_currentState == null) return;

            for (var l_i = 0; l_i < m_currentStateConditionsAmount; l_i++)
            {
                if (!m_currentState.StateConditions[l_i].CompleteCondition(m_model))
                    continue;

                ChangeState(m_currentState.ExitStates[l_i].MyState.GetType());
                return;
            }

            m_currentState.MyState.ExecuteState(m_model);
        }

        private void ChangeState(Type p_newStateType)
        {
            if (!m_statesDictionary.TryGetValue(p_newStateType, out var l_newStateData))
                return;

            m_currentState.MyState.ExitState(m_model);
            m_currentState = l_newStateData;
            m_currentState.MyState.EnterState(m_model);
            m_currentStateConditionsAmount = m_currentState.StateConditions.Count;
        }

        public StateData GetCurrentState() => m_currentState;

        public List<StateData> GetAllStates() => allStatesData;

        public void AddState(StateData p_data) => allStatesData.Add(p_data);
        
        public void ClearStates()
        {
            if (allStatesData == null)
                allStatesData = new List<StateData>();
            else
                allStatesData.Clear();
    
            // Asegurarse de limpiar cualquier referencia al estado actual
            m_currentState = null;
    
            // Marcar como modificado para el guardado
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }
}