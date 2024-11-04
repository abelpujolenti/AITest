using UnityEngine;

namespace AI.Combat
{
    public abstract class AIAttackCollider : MonoBehaviour
    {
        public abstract void SetAttackTargets(int targetsLayerMask);

        protected Quaternion _parentRotation;

        protected abstract void OnEnable();

        public void SetParent(Transform parentTransform)
        {
            transform.parent = parentTransform;
            _parentRotation = parentTransform.rotation;
        }

        protected void MoveToPosition(Vector3 position)
        {
            gameObject.transform.localPosition = position;
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}