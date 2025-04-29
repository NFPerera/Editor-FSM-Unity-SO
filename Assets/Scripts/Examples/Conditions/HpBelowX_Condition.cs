using Extensions.FSM.Base.StateMachine;
using Extensions.FSM.Models;
using UnityEngine;


[CreateAssetMenu(fileName = "HpBelowX", menuName = "Main/FSM/Conditions/HpBelowX")]
public class HpBelowX_Condition : StateCondition
{
    
    [SerializeField] private float HpTreshold;
    public override bool CompleteCondition(IUseFsm p_model)
    {
        return true;
    }
}
