using ECS.Components.AI.Combat;
using ECS.Entities.AI.Combat;
using UnityEngine;

namespace AI.Combat.Ally
{
    public class AIAllyConeAttackCollider : AIAllyAttackCollider
    {
        private AllyConeAttackComponent _coneAttackComponent;

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
            transform.rotation = 
                _parentRotation * Quaternion.LookRotation(_coneAttackComponent.GetDirection().normalized, Vector3.up);
        }

        public override void SetAttackTargets(int targetsLayerMask)
        {
            _sphereCollider.includeLayers = targetsLayerMask;
            _sphereCollider.excludeLayers = ~targetsLayerMask;
        }

        public void SetConeAttackComponent(AllyConeAttackComponent coneAttackComponent)
        {
            _allyID = coneAttackComponent.GetAllyID();
            
            _sphereCollider = gameObject.AddComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            
            _coneAttackComponent = coneAttackComponent;
            
            Rotate();
        }

        public override void StartInflictingDamage()
        {
            foreach (AIEnemy enemy in _combatAgentsTriggering)
            {
                InflictDamageToAnAlly(enemy);
            }
        }

        private void InflictDamageToAnAlly(AIEnemy enemy)
        {
            enemy.OnReceiveDamage(new AllyDamageComponent(_coneAttackComponent.GetDamage(),
                _coneAttackComponent.GetStressDamage()));
        }

        private void OnTriggerEnter(Collider other)
        {
            AIEnemy targetEnemy = other.GetComponent<AIEnemy>();

            if (_combatAgentsTriggering.Contains(targetEnemy))
            {
                return;
            }
            
            _combatAgentsTriggering.Add(targetEnemy);
        }
    }
}