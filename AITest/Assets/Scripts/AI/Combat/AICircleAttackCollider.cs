using System;
using ECS.Components.AI.Combat;
using UnityEngine;

namespace AI.Combat
{
    public class AICircleAttackCollider : AIAttackCollider
    {
        private CircleAttackComponent _circleAttackComponent;

        private SphereCollider _sphereCollider;

        protected override void OnEnable()
        {
            if (_circleAttackComponent == null)
            {
                return;
            }
            
            MoveToPosition(_circleAttackComponent.GetRelativePosition());

            if (!_circleAttackComponent.IsAttachedToAttacker())
            {
                return;
            }

            transform.parent = null;
        }

        public override void SetAttackTargets(int targetsLayerMask)
        {
            _sphereCollider.includeLayers = targetsLayerMask;
            _sphereCollider.excludeLayers = ~targetsLayerMask;
        }

        public void SetCircleAttackComponent(CircleAttackComponent circleAttackComponent)
        {
            _sphereCollider = gameObject.AddComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            
            _circleAttackComponent = circleAttackComponent;
            _sphereCollider.radius = _circleAttackComponent.GetRadius();
        }

        private void OnTriggerEnter(Collider other)
        {
            throw new NotImplementedException();
        }
    }
}