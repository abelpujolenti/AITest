using Interfaces.AI.UBS.Ally;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIAllyContext : AICombatAgentContext, IAllyFollowPlayerUtility, IAllyChooseNewRivalUtility,
        IAllyGetCloserToRivalUtility, IAllyAttackUtility, IAllyFleeUtility, IAllyDodgeAttackUtility, IAllyHelpAllyUtility 
    {
        private uint basicAttackDamage;
        private uint oncomingAttackDamage;
        private uint enemyHealth;
        private uint threatGroupOfTarget = 0;
        
        private float basicStressDamage;
        private float basicAttackMaximumRange;
        private float moralWeight;
        private float threatWeightOfTarget = 0;
        private float enemyMaximumStress;
        private float enemyCurrentStress;
        
        private bool isUnderThreat;
        private bool isUnderAttack;
        private bool isAnotherAllyUnderThreat;
        private bool isAirborne;
        private bool wasRetreatOrderUsed;
        private bool wasAttackOrderUsed;
        private bool wasFleeOrderUsed;

        public AIAllyContext(uint totalHealth, Transform agentTransform, float basicAttackMaximumRange, uint basicAttackDamage, 
            float basicStressDamage, float moralWeight) : base(totalHealth, agentTransform)
        {
            this.basicAttackMaximumRange = basicAttackMaximumRange;
            this.basicAttackDamage = basicAttackDamage;
            this.basicStressDamage = basicStressDamage;
            this.moralWeight = moralWeight;
        }

        public uint GetBasicAttackDamage()
        {
            return basicAttackDamage;
        }

        public void SetRivalHealth(uint rivalHealth)
        {
            enemyHealth = rivalHealth;
        }

        public uint GetRivalHealth()
        {
            return enemyHealth;
        }

        public void SetThreatGroupOfTarget(uint threatGroupOfTarget)
        {
            this.threatGroupOfTarget = threatGroupOfTarget;
        }

        public uint GetThreatGroupOfTarget()
        {
            return threatGroupOfTarget;
        }

        public float GetBasicStressDamage()
        {
            return basicStressDamage;
        }

        public float GetBasicAttackMaximumRange()
        {
            return basicAttackMaximumRange;
        }

        public void SetMoralWeight(float moralWeight)
        {
            this.moralWeight = moralWeight;
        }

        public float GetMoralWeight()
        {
            return moralWeight;
        }

        public void SetThreatWeightOfTarget(float threatWeightOfTarget)
        {
            this.threatWeightOfTarget = threatWeightOfTarget;
        }

        public float GetThreatWeightOfTarget()
        {
            return threatWeightOfTarget;
        }

        public void SetRivalMaximumStress(float rivalMaximumStress)
        {
            enemyMaximumStress = rivalMaximumStress;
        }

        public float GetRivalMaximumStress()
        {
            return enemyMaximumStress;
        }

        public void SetRivalCurrentStress(float rivalCurrentStress)
        {
            enemyCurrentStress = rivalCurrentStress;
        }

        public float GetRivalCurrentStress()
        {
            return enemyCurrentStress;
        }

        public void SetIsUnderThreat(bool isUnderThreat)
        {
            this.isUnderThreat = isUnderThreat;
        }

        public bool IsUnderThreat()
        {
            return isUnderThreat;
        }

        public void SetIsUnderAttack(bool isUnderAttack)
        {
            this.isUnderAttack = isUnderAttack;
        }

        public bool IsUnderAttack()
        {
            return isUnderAttack;
        }

        public void SetOncomingAttackDamage(uint oncomingAttackDamage)
        {
            this.oncomingAttackDamage = oncomingAttackDamage;
        }

        public uint GetOncomingAttackDamage()
        {
            return oncomingAttackDamage;
        }

        public void SetIsAnotherAllyUnderThreat(bool isAnotherAllyUnderThreat)
        {
            this.isAnotherAllyUnderThreat = isAnotherAllyUnderThreat;
        }

        public bool IsAnotherAllyUnderThreat()
        {
            return isAnotherAllyUnderThreat;
        }

        public void SetIsAirborne(bool isAirborne)
        {
            this.isAirborne = isAirborne;
        }

        public bool IsAirborne()
        {
            return isAirborne;
        }

        public void SetIsInRetreatState(bool isInRetreatState)
        {
            wasRetreatOrderUsed = isInRetreatState;
        }

        public bool IsInRetreatState()
        {
            return wasRetreatOrderUsed;
        }

        public void SetIsInAttackState(bool isInAttackState)
        {
            wasAttackOrderUsed = isInAttackState;
        }

        public bool IsInAttackState()
        {
            return wasAttackOrderUsed;
        }

        public void SetIsInFleeState(bool isInFleeState)
        {
            wasFleeOrderUsed = isInFleeState;
        }

        public bool IsInFleeState()
        {
            return wasFleeOrderUsed;
        }

        public override float GetWeight()
        {
            return moralWeight;
        }
    }
}