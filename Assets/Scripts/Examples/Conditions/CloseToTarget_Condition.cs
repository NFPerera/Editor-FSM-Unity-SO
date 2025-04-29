using System.Collections;
using System.Collections.Generic;
using Extensions.FSM.Base.StateMachine;
using Extensions.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "CloseToTarget", menuName = "Main/FSM/Conditions/CloseToTarget")]
public class CloseToTarget_Condition : StateCondition
{
    [SerializeField] private float distanceToTarget;
    public override bool CompleteCondition(IUseFsm p_model)
    {
        return true;
    }
}
