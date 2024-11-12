using System;
using ECS.Components.AI.Combat;
using ECS.Entities.AI.Combat;
using Managers;
using Unity.AI.Navigation;
using UnityEngine;

namespace AI.Combat.Enemy
{
    public class AIEnemyRectangleAttackCollider : AIEnemyAttackCollider
    {
        [SerializeField] private BoxCollider _boxCollider;
        
        private RectangleAttackComponent _rectangleAttackComponent;

        protected override void OnEnable()
        {
            if (_rectangleAttackComponent == null)
            {
                return;
            }

            _isWarning = true;
            
            MoveToPosition(_rectangleAttackComponent.GetRelativePosition());
            Rotate();

            if (!_rectangleAttackComponent.IsAttachedToAttacker())
            {
                return;
            }

            transform.parent = null;
        }

        protected override void OnDisable()
        {
            foreach (AIAlly ally in _combatAgentsTriggering)
            {
                ally.FreeOfWarnArea(_rectangleAttackComponent, this);
            }
            base.OnDisable();
        }

        private void Rotate()
        {
            transform.rotation = 
                _parentRotation * Quaternion.LookRotation(_rectangleAttackComponent.GetDirection().normalized, Vector3.up);
        }

        public override void SetAttackTargets(int targetsLayerMask)
        {
            _boxCollider.includeLayers = targetsLayerMask;
            _boxCollider.excludeLayers = ~targetsLayerMask;
        }

        public void SetRectangleAttackComponent(RectangleAttackComponent rectangleAttackComponent)
        {
            _rectangleAttackComponent = rectangleAttackComponent;

            float height = _rectangleAttackComponent.GetHeight();
            float width = _rectangleAttackComponent.GetWidth();
            float length = _rectangleAttackComponent.GetLength();
            
            Vector3 sizes = new Vector3(width, height, length);
            
            _boxCollider.size = sizes;

            foreach (NavMeshModifierVolume navMeshModifierVolume in _navMeshModifierVolumes)
            {
                navMeshModifierVolume.size = sizes;
            }

            Vector3 center = new Vector3
            {
                x = Convert.ToInt16(!_rectangleAttackComponent.IsRelativePositionXCenterOfColliderX()) * (width / 2),
                y = Convert.ToInt16(!_rectangleAttackComponent.IsRelativePositionYCenterOfColliderY()) * (height / 2),
                z = Convert.ToInt16(!_rectangleAttackComponent.IsRelativePositionZCenterOfColliderZ()) * (length / 2)
            };

            _boxCollider.center = center;
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
            ally.FreeOfWarnArea(_rectangleAttackComponent, this);
            ally.OnReceiveDamage(new DamageComponent(_rectangleAttackComponent.GetDamage()));
        }

        public override Vector2[] GetCornerPoints()
        {
            Vector3 center = _boxCollider.center;
            
            Vector3 size = _boxCollider.size;

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
                Vector3 worldCorner = _boxCollider.transform.TransformPoint(localCorners[i]);
                corners[i] = new Vector2(worldCorner.x + center.x, worldCorner.z + center.z);
            }
            
            return corners;
        }

        private void OnTriggerEnter(Collider other)
        {
            AIAlly targetAlly = other.GetComponent<AIAlly>();

            if (_isWarning)
            {
                targetAlly.WarnOncomingDamage(_rectangleAttackComponent, this);    
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
            
            targetAlly.FreeOfWarnArea(_rectangleAttackComponent, this);    
            _combatAgentsTriggering.Remove(targetAlly);
        }

        private void OnDrawGizmos()
        {
            Vector2[] corners = GetCornerPoints();
            
            Gizmos.color = Color.red;

            for (int i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawSphere(new Vector3(corners[i].x, 0, corners[i].y), 0.1f);
                Gizmos.DrawLine(new Vector3(corners[i].x, 0, corners[i].y), new Vector3(corners[(i + 1) % corners.Length].x, 0, corners[(i + 1) % corners.Length].y));
            }
        }
    }
}