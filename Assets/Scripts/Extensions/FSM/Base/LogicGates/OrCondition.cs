using Extensions.FSM.Base.StateMachine;
using Extensions.FSM.Models;
using UnityEngine;

namespace Extensions.FSM.Base.LogicGates
{
    [CreateAssetMenu(fileName = "OrCondition", menuName = "Main/FSM/LogicGates/OR")]
    public class OrCondition : StateCondition
    {
        [SerializeField] private StateCondition conditionOne;
        [SerializeField] private StateCondition conditionTwo;

        public override bool CompleteCondition(IUseFsm p_model)
        {
            return conditionOne.CompleteCondition(p_model) || conditionTwo.CompleteCondition(p_model);
        }
    }
}