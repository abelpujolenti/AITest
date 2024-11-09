using UnityEngine;

namespace AI.Combat
{
    public abstract class AIAttackCollider : MonoBehaviour
    {
        public abstract void SetAttackTargets(int targetsLayerMask);
        public abstract void StartInflictingDamage();

        protected Quaternion _parentRotation;

        protected abstract void OnEnable();
        protected abstract void OnDisable();

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