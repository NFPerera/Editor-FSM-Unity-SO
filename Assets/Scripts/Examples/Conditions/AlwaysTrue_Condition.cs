using System.Collections;
using System.Collections.Generic;
using Extensions.FSM.Base.StateMachine;
using Extensions.FSM.Models;
using UnityEngine;


[CreateAssetMenu(fileName = "AlwaysTrue", menuName = "Main/FSM/Conditions/AlwaysTrue")]
public class AlwaysTrue_Condition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        return true;
    }
}
