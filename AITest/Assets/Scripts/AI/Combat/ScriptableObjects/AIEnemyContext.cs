using AI.Combat.Enemy;
using Interfaces.AI.UBS.BaseInterfaces.Property;
using Interfaces.AI.UBS.Enemy;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIEnemyContext : AICombatAgentContext, IEnemyPatrolUtility, IEnemyChooseNewRivalUtility, 
        IEnemyGetCloserToRivalUtility, IEnemyAttackUtility, IEnemyFleeUtility, IStunned
    {
        private float _threatLevel;
        private float _currentThreatGroupWeight;
        private float _originalThreatGroupInfluenceRadius;
        private float _maximumStress;
        private float _currentStress;
        private float _stunDuration;

        private bool _isStunned;

        public AIEnemyContext(uint totalHealth, uint currentGroup, float radius, float sightMaximumDistance, float minimumRangeToAttack, 
            float maximumRangeToAttack, Transform agentTransform, float threatLevel, float originalThreatGroupInfluenceRadius, 
            float maximumStress, float stunDuration) : base(totalHealth, currentGroup, radius, sightMaximumDistance, minimumRangeToAttack, 
            maximumRangeToAttack, agentTransform)
        {
            _repeatableActions.Add((uint)AIEnemyAction.CHOOSE_NEW_RIVAL);
            _repeatableActions.Add((uint)AIEnemyAction.ROTATE);
            _repeatableActions.Add((uint)AIEnemyAction.ATTACK);
            
            _threatLevel = threatLevel;
            _currentThreatGroupWeight = _threatLevel;
            _originalThreatGroupInfluenceRadius = originalThreatGroupInfluenceRadius;
            _maximumStress = maximumStress;
            _stunDuration = stunDuration;
        }

        public void SetCurrentThreatGroupWeight(float currentThreatGroupWeight)
        {
            _currentThreatGroupWeight = currentThreatGroupWeight;
        }

        public float GetCurrentThreatGroupWeight()
        {
            return _currentThreatGroupWeight;
        }

        public float GetOriginalThreatGroupInfluenceRadius()
        {
            return _originalThreatGroupInfluenceRadius;
        }

        public float GetMaximumStress()
        {
            return _maximumStress;
        }

        public void SetCurrentStress(float currentStress)
        {
            _currentStress = currentStress;

            if (_currentStress < _maximumStress)
            {
                return;
            }

            _isStunned = true;
            _currentStress = 0;
        }

        public float GetCurrentStress()
        {
            return _currentStress;
        }

        public float GetStunDuration()
        {
            return _stunDuration;
        }

        public void SetIsStunned(bool isStunned)
        {
            _isStunned = isStunned;
        }

        public bool IsStunned()
        {
            return _isStunned;
        }

        public override float GetWeight()
        {
            return _threatLevel;
        }
    }
}