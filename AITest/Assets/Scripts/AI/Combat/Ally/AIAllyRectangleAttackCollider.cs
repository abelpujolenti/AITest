using System;
using ECS.Components.AI.Combat;
using ECS.Entities.AI.Combat;
using Managers;
using UnityEngine;

namespace AI.Combat.Ally
{
    public class AIAllyRectangleAttackCollider : AIAllyAttackCollider
    {
        private AllyRectangleAttackComponent _rectangleAttackComponent; 

        private BoxCollider _boxCollider;

        protected override void OnEnable()
        {
            if (_rectangleAttackComponent == null)
            {
                return;
            }
            
            MoveToPosition(_rectangleAttackComponent.GetRelativePosition());
            Rotate();

            if (!_rectangleAttackComponent.IsAttachedToAttacker())
            {
                return;
            }

            transform.parent = null;
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

        public void SetRectangleAttackComponent(AllyRectangleAttackComponent rectangleAttackComponent)
        {
            _allyID = rectangleAttackComponent.GetAllyID();
            
            _boxCollider = gameObject.AddComponent<BoxCollider>();
            _boxCollider.isTrigger = true;
            
            _rectangleAttackComponent = rectangleAttackComponent;

            float height = _rectangleAttackComponent.GetHeight();
            float width = _rectangleAttackComponent.GetWidth();
            float length = _rectangleAttackComponent.GetLength();
            
            _boxCollider.size = new Vector3(width, height, length);

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
            foreach (AIEnemy enemy in _combatAgentsTriggering)
            {
                InflictDamageToAnAlly(enemy);
            }
        }

        private void InflictDamageToAnAlly(AIEnemy enemy)
        {
            enemy.OnReceiveDamage(new AllyDamageComponent(_rectangleAttackComponent.GetDamage(),
                _rectangleAttackComponent.GetStressDamage()));
        }

        private void OnTriggerEnter(Collider other)
        {
            AIEnemy targetEnemy = other.GetComponent<AIEnemy>();

            if (_combatAgentsTriggering.Contains(targetEnemy))
            {
                return;
            }
            
            CombatManager.Instance.CheckEnemyThreatGroup(_allyID, targetEnemy.GetContext().GetCurrentGroup());
            
            _combatAgentsTriggering.Add(targetEnemy);
        }

        private void OnDrawGizmos()
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
            
            Gizmos.color = Color.green;

            for (int i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawSphere(new Vector3(corners[i].x, 0, corners[i].y), 0.1f);
                Gizmos.DrawLine(new Vector3(corners[i].x, 0, corners[i].y), new Vector3(corners[(i + 1) % corners.Length].x, 0, corners[(i + 1) % corners.Length].y));
            }
        }
    }
}