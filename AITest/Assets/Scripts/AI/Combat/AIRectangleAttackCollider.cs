using System;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using ECS.Entities.AI.Combat;
using UnityEngine;

namespace AI.Combat
{
    public class AIRectangleAttackCollider : AIAttackCollider
    {
        private RectangleAttackComponent _rectangleAttackComponent;

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
            transform.rotation = _parentRotation * 
                                 Quaternion.LookRotation(_rectangleAttackComponent.GetDirection().normalized, Vector3.up);
        }

        public override void SetAttackTargets(int targetsLayerMask)
        {
            _boxCollider.includeLayers = targetsLayerMask;
            _boxCollider.excludeLayers = ~targetsLayerMask;
        }

        public void SetRectangleAttackComponent(RectangleAttackComponent rectangleAttackComponent)
        {
            gameObject.name = rectangleAttackComponent.GetCurrentTimeToFinishCast().ToString();
            
            _boxCollider = gameObject.AddComponent<BoxCollider>();
            _boxCollider.isTrigger = true;
            
            _rectangleAttackComponent = rectangleAttackComponent;

            float width = _rectangleAttackComponent.GetWidth();
            float height = _rectangleAttackComponent.GetHeight();
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

        private void OnTriggerEnter(Collider other)
        {
            //TODO WARN ALLY
            AICombatAgentEntity<AICombatAgentContext> aiCombatAgent = other.GetComponent<AICombatAgentEntity<AICombatAgentContext>>();
        }

        private void OnTriggerExit(Collider other)
        {
            //TODO STOP WARNING ALLY
            AICombatAgentEntity<AICombatAgentContext> aiCombatAgent = other.GetComponent<AICombatAgentEntity<AICombatAgentContext>>();
        }
    }
}