using System.Collections;
using System.Collections.Generic;
using Extensions.FSM.Base.StateMachine;
using Extensions.FSM.Models;
using UnityEngine;


[CreateAssetMenu(fileName = "LineOfSightToPlayer", menuName = "Main/FSM/Conditions/LineOfSightToPlayer")]
public class LineOfSightToPlayer_Condition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        return true;
    }
}
