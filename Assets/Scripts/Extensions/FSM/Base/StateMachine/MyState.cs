using Extensions.FSM.Models;
using UnityEngine;

namespace Extensions.FSM.Base.StateMachine
{
    public abstract class MyState : ScriptableObject
    {
        public string StateName;

        public virtual void EnterState(IUseFsm p_model)
        { }

        public abstract void ExecuteState(IUseFsm p_model);

        public virtual void ExitState(IUseFsm p_model)
        { }
    }
}