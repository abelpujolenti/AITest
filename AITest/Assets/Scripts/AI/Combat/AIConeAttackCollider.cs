using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using ECS.Entities.AI.Combat;
using UnityEngine;

namespace AI.Combat
{
    public class AIConeAttackCollider : AIAttackCollider
    {
        private ConeAttackComponent _coneAttackComponent;

        private SphereCollider _sphereCollider;

        protected override void OnEnable()
        {
            if (_coneAttackComponent == null)
            {
                return;
            }
            
            MoveToPosition(_coneAttackComponent.GetRelativePosition());
            Rotate();

            if (!_coneAttackComponent.IsAttachedToAttacker())
            {
                return;
            }

            transform.parent = null;
        }

        private void Rotate()
        {
            transform.rotation = _parentRotation *
                                 Quaternion.LookRotation(_coneAttackComponent.GetDirection().normalized, Vector3.up);
        }

        public override void SetAttackTargets(int targetsLayerMask)
        {
            _sphereCollider.includeLayers = targetsLayerMask;
            _sphereCollider.excludeLayers = ~targetsLayerMask;
        }

        public void SetConeAttackComponent(ConeAttackComponent coneAttackComponent)
        {
            _sphereCollider = gameObject.AddComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            
            _coneAttackComponent = coneAttackComponent;
            
            Rotate();
        }

        private void OnTriggerEnter(Collider other)
        {
            //TODO
            AICombatAgentEntity<AICombatAgentContext> aiCombatAgent = other.GetComponent<AICombatAgentEntity<AICombatAgentContext>>();
        }

        private void OnTriggerExit(Collider other)
        {
            //TODO
            AICombatAgentEntity<AICombatAgentContext> aiCombatAgent = other.GetComponent<AICombatAgentEntity<AICombatAgentContext>>();
        }
    }
}