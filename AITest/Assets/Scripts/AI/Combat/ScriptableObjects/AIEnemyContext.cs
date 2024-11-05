using Interfaces.AI.UBS.Enemy;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIEnemyContext : AICombatAgentContext, IEnemyPatrolUtility, IEnemyChooseNewRivalUtility, IEnemyGetCloserToRivalUtility,
        IEnemyAttackUtility, IEnemyFleeUtility
    {
        private uint currentThreatGroup;
        private uint currentThreatGroupWeight;
        
        private float threatLevel;
        private float maximumStress;
        private float currentStress;

        public AIEnemyContext(uint totalHealth, Transform agentTransform, float threatLevel, float maximumStress) : base(totalHealth, agentTransform)
        {
            this.threatLevel = threatLevel;
            this.maximumStress = maximumStress;
        }

        public void SetCurrentThreatGroup(uint currentThreatGroup)
        {
            this.currentThreatGroup = currentThreatGroup;
        }

        public uint GetCurrentThreatGroup()
        {
            return currentThreatGroup;
        }

        public void SetCurrentThreatGroupWeight(uint currentThreatGroupWeight)
        {
            this.currentThreatGroupWeight = currentThreatGroupWeight;
        }

        public uint GetCurrentThreatGroupWeight()
        {
            return currentThreatGroupWeight;
        }

        public float GetMaximumStress()
        {
            return maximumStress;
        }

        public void SetCurrentStress(float currentStress)
        {
            this.currentStress = currentStress;
        }

        public float GetCurrentStress()
        {
            return currentStress;
        }

        public override float GetWeight()
        {
            return threatLevel;
        }
    }
}