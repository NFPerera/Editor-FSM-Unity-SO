using UnityEngine;

namespace Extensions.FSM.Models
{
    public interface IUseFsm
    {
        public Transform GetModelTransform();

        void UpdateFsm();

        void SetTargetTransform(Transform target);

        Transform GetTargetTransform();
    }
}