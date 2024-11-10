using System.Collections.Generic;
using System.Linq;
using ECS.Components.AI.Combat;
using ECS.Entities.AI.Combat;
using Managers;
using Unity.AI.Navigation;
using UnityEngine;
using Utilities;

namespace AI.Combat.Enemy
{
    public class AIEnemyCircleAttackCollider : AIEnemyAttackCollider
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

        protected override void OnDisable()
        {
            foreach (AIAlly ally in _combatAgentsTriggering)
            {
                ally.FreeOfWarnArea(_circleAttackComponent, this);
            }
            base.OnDisable();
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
            float radius = _circleAttackComponent.GetRadius();
            float height = _circleAttackComponent.GetHeight();
            
            _sphereCollider.radius = radius;

            foreach (NavMeshModifierVolume navMeshModifierVolume in _navMeshModifierVolumes)
            {
                navMeshModifierVolume.size = new Vector3(radius, height, 4);
            }
        }

        public override void StartInflictingDamage()
        {
            _isWarning = false;

            foreach (AIAlly ally in _combatAgentsTriggering)
            {
                InflictDamageToAnAlly(ally);
            }
        }

        private void InflictDamageToAnAlly(AIAlly ally)
        {
            ally.OnReceiveDamage(new DamageComponent(_circleAttackComponent.GetDamage()));
        }

        public override Vector2[] GetCornerPoints()
        {
            List<Vector2> corners = new List<Vector2>();

            for (int i = 0; i < _navMeshModifierVolumes.Count; i++)
            {
                Vector2[] currentCorners = GetGivenVolumeCornerPoints(_navMeshModifierVolumes[i]);

                for (int j = 0; j < 4; j++)
                {
                    corners.Add(currentCorners[j]);
                }
            }

            return PolygonUtilities.OrderVerticesCounterClockwise(corners).ToArray();
        }

        private Vector2[] GetGivenVolumeCornerPoints(NavMeshModifierVolume navMeshModifierVolume)
        {
            Vector3 size = navMeshModifierVolume.size;

            Vector3 halfExtents = size / 2f;
            
            Vector3[] localCorners = new Vector3[]
            {
                new Vector3(-halfExtents.x, 0, -halfExtents.z),
                new Vector3(halfExtents.x, 0, -halfExtents.z),
                new Vector3(halfExtents.x, 0, halfExtents.z),
                new Vector3(-halfExtents.x, 0, halfExtents.z),
            };

            Vector2[] corners = new Vector2[localCorners.Length];

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector3 worldCorner = navMeshModifierVolume.transform.TransformPoint(localCorners[i]);
                corners[i] = new Vector2(worldCorner.x, worldCorner.z);
            }
            
            return corners.ToArray();
        }

        private void OnTriggerEnter(Collider other)
        {
            AIAlly targetAlly = other.GetComponent<AIAlly>();

            if (_isWarning)
            {
                targetAlly.WarnOncomingDamage(_circleAttackComponent, this);    
            }
            else
            {
                InflictDamageToAnAlly(targetAlly);   
            }
            
            _combatAgentsTriggering.Add(targetAlly);

            if (_isSubscribed)
            {
                return;
            }

            _isSubscribed = true;
            CombatManager.Instance.SubscribeToRebake(this);
        }

        private void OnTriggerExit(Collider other)
        {
            AIAlly targetAlly = other.GetComponent<AIAlly>();

            if (!_isWarning)
            {
                return;
            }
            
            targetAlly.FreeOfWarnArea(_circleAttackComponent, this);    
            _combatAgentsTriggering.Remove(targetAlly);
        }
    }
}