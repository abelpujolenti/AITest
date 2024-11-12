using ECS.Components.AI.Combat;
using ECS.Entities.AI.Combat;
using UnityEngine;

namespace AI.Combat.Ally
{
    public class AIAllyCircleAttackCollider : AIAllyAttackCollider
    {
        private AllyCircleAttackComponent _circleAttackComponent;

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

        protected override void OnDisable()
        {
            _combatAgentsTriggering.Clear();
        }

        public override void SetAttackTargets(int targetsLayerMask)
        {
            _sphereCollider.includeLayers = targetsLayerMask;
            _sphereCollider.excludeLayers = ~targetsLayerMask;
        }

        public void SetCircleAttackComponent(AllyCircleAttackComponent circleAttackComponent)
        {
            _allyID = circleAttackComponent.GetAllyID();
            
            _sphereCollider = gameObject.AddComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            
            _circleAttackComponent = circleAttackComponent;
            _sphereCollider.radius = _circleAttackComponent.GetRadius();
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
            enemy.OnReceiveDamage(new AllyDamageComponent(_circleAttackComponent.GetDamage(),
                _circleAttackComponent.GetStressDamage()));
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