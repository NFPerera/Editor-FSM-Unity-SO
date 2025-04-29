using System.Collections;
using System.Collections.Generic;
using Extensions.FSM.Base.StateMachine;
using Extensions.FSM.Models;
using UnityEngine;


[CreateAssetMenu(fileName = "TargetIsInRange", menuName = "Main/FSM/Conditions/TargetIsInRange")]
public class TargetIsInRange_Condition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        return true;
    }
}
