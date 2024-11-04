using Interfaces.AI.Combat;
using Interfaces.AI.UBS.BaseInterfaces;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public abstract class AICombatAgentContext : ITotalHealth, IHealth, IIsSeeingARival, IHasATarget, IIsFighting, IRivalTransform, 
        IAgentTransform, IStatWeight
    {
        public uint lastActionIndex = 10;
        
        private uint totalHealth;
        public uint health;

        public bool isSeeingARival;
        public bool hasATarget;
        public bool isFighting;

        public Transform agentTransform;
        public Transform rivalTransform;

        protected AICombatAgentContext(uint totalHealth, Transform agentTransform)
        {
            this.totalHealth = totalHealth;
            health = totalHealth;
            this.agentTransform = agentTransform;
        }

        public uint GetTotalHealth()
        {
            return totalHealth;
        }

        public uint GetHealth()
        {
            return health;
        }

        public bool IsSeeingARival()
        {
            return isSeeingARival;
        }

        public bool HasATarget()
        {
            return hasATarget;
        }

        public bool IsFighting()
        {
            return isFighting;
        }

        public Transform GetRivalTransform()
        {
            return rivalTransform;
        }

        public Transform GetAgentTransform()
        {
            return agentTransform;
        }

        public abstract float GetWeight();
    }
}