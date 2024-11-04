using Interfaces.AI.UBS.Ally;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIAllyContext : AICombatAgentContext, IAllyFollowPlayerUtility, IAllyLookForRivalUtility, 
        IAllyGetCloserToRivalUtility, IAllyAttackUtility, IAllyFleeUtility, IAllyDodgeAttackUtility, IAllyHelpAllyUtility 
    {
        private uint basicAttackDamage;
        public uint oncomingAttackDamage;
        public uint enemyHealth;
        public uint threatGroupOfTarget = 0;
        
        private float basicStressDamage;
        private float basicAttackMaximumRange;
        public float distanceToEnemy;
        public float moralWeight;
        public float threatWeightOfTarget = 0;
        public float enemyStressRemainingToStun;
        
        public bool isUnderThreat;
        public bool isUnderAttack;
        public bool isAnotherAllyUnderThreat;
        public bool isAirborne;
        public bool isInRetreatState;
        public bool isInAttackState;
        public bool isInFleeState;

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

        public uint GetRivalHealth()
        {
            return enemyHealth;
        }

        public float GetBasicStressDamage()
        {
            return basicStressDamage;
        }

        public float GetBasicAttackMaximumRange()
        {
            return basicAttackMaximumRange;
        }

        public float GetDistanceToEnemy()
        {
            return distanceToEnemy;
        }

        public float GetMoralWeight()
        {
            return moralWeight;
        }

        public float GetThreatWeightOfTarget()
        {
            return threatWeightOfTarget;
        }

        public float GetRivalStressRemainingToStun()
        {
            return enemyStressRemainingToStun;
        }

        public bool IsUnderThreat()
        {
            return isUnderThreat;
        }

        public bool IsUnderAttack()
        {
            return isUnderAttack;
        }

        public uint GetOncomingAttackDamage()
        {
            return oncomingAttackDamage;
        }

        public bool IsAnotherAllyUnderThreat()
        {
            return isAnotherAllyUnderThreat;
        }

        public bool IsAirborne()
        {
            return isAirborne;
        }

        public bool IsInRetreatState()
        {
            return isInRetreatState;
        }

        public bool IsInAttackState()
        {
            return isInAttackState;
        }

        public bool IsInFleeState()
        {
            return isInFleeState;
        }

        public override float GetWeight()
        {
            return moralWeight;
        }
    }
}