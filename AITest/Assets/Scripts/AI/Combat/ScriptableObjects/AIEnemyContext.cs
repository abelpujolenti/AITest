using Interfaces.AI.UBS.Enemy;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIEnemyContext : AICombatAgentContext, IEnemyPatrolUtility, IEnemyChooseNewRivalUtility, IEnemyGetCloserToRivalUtility,
        IEnemyAttackUtility, IEnemyFleeUtility
    {
        private uint _currentThreatGroup;
        
        private float _threatLevel;
        private float _currentThreatGroupWeight;
        private float _maximumStress;
        private float _currentStress;
        private float _minimumRangeToAttack;
        private float _maximumRangeToAttack;

        public AIEnemyContext(uint totalHealth, float radius, float sightMaximumDistance, Transform agentTransform, float threatLevel, 
            float maximumStress, float minimumRangeToAttack, float maximumRangeToAttack) : 
            base(totalHealth, radius, sightMaximumDistance, agentTransform)
        {
            _threatLevel = threatLevel;
            _currentThreatGroupWeight = _threatLevel;
            _maximumStress = maximumStress;
            _minimumRangeToAttack = minimumRangeToAttack;
            _maximumRangeToAttack = maximumRangeToAttack;
        }

        public void SetCurrentThreatGroup(uint currentThreatGroup)
        {
            _currentThreatGroup = currentThreatGroup;
        }

        public uint GetCurrentThreatGroup()
        {
            return _currentThreatGroup;
        }

        public void SetCurrentThreatGroupWeight(float currentThreatGroupWeight)
        {
            _currentThreatGroupWeight = currentThreatGroupWeight;
        }

        public float GetCurrentThreatGroupWeight()
        {
            return _currentThreatGroupWeight;
        }

        public float GetMaximumStress()
        {
            return _maximumStress;
        }

        public void SetCurrentStress(float currentStress)
        {
            _currentStress = currentStress;
        }

        public float GetCurrentStress()
        {
            return _currentStress;
        }

        public float GetMinimumRangeToAttack()
        {
            return _minimumRangeToAttack;
        }

        public float GetMaximumRangeToAttack()
        {
            return _maximumRangeToAttack;
        }

        public override float GetWeight()
        {
            return _threatLevel;
        }
    }
}