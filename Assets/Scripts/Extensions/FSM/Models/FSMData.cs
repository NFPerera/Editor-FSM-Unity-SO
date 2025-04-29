using System.Collections.Generic;
using Extensions.FSM.Base.StateMachine;
using UnityEngine;

namespace Extensions.FSM.Models
{
    public class FSMData : ScriptableObject
    {
        [field: SerializeField] public List<StateData> AllStatesData { get; private set; }
    }
}