using Extensions.FSM.Models;
using UnityEngine;

namespace Extensions.FSM.Base.StateMachine
{
    public abstract class StateCondition : ScriptableObject
    {
        public abstract bool CompleteCondition(IUseFsm p_model);
    }
}