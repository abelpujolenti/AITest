using Interfaces.AI.Combat;
using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public abstract class AICombatAgentContext : IGetTotalHealth, ILastActionIndex, IHealth, IRivalIndex, IDistanceToRival, ISeeingARival, 
        ITarget, IFighting, IAttacking, IVectorToRival, IRivalTransform, IGetAgentTransform, IStatWeight
    {
        private uint lastActionIndex = 10;
        
        private uint totalHealth;
        private uint health;
        private uint rivalIndex;

        private float distanceToRival;

        private bool isSeeingARival;
        private bool hasATarget;
        private bool isFighting;
        private bool isAttacking;

        private Vector3 vectorToRival;

        private Transform agentTransform;
        private Transform rivalTransform;

        protected AICombatAgentContext(uint totalHealth, Transform agentTransform)
        {
            this.totalHealth = totalHealth;
            health = totalHealth;
            this.agentTransform = agentTransform;
        }

        public void SetLastActionIndex(uint lastActionIndex)
        {
            this.lastActionIndex = lastActionIndex;
        }

        public uint GetLastActionIndex()
        {
            return lastActionIndex;
        }

        public uint GetTotalHealth()
        {
            return totalHealth;
        }

        public void SetHealth(uint health)
        {
            this.health = health;
        }

        public uint GetHealth()
        {
            return health;
        }

        public void SetRivalIndex(uint rivalIndex)
        {
            this.rivalIndex = rivalIndex;
        }

        public uint GetRivalIndex()
        {
            return rivalIndex;
        }

        public void SetDistanceToRival(float distanceToRival)
        {
            this.distanceToRival = distanceToRival;
        }

        public float GetDistanceToRival()
        {
            return distanceToRival;
        }

        public void SetIsSeeingARival(bool isSeeingARival)
        {
            this.isSeeingARival = isSeeingARival;
        }

        public bool IsSeeingARival()
        {
            return isSeeingARival;
        }

        public void SetHasATarget(bool hasATarget)
        {
            this.hasATarget = hasATarget;
        }

        public bool HasATarget()
        {
            return hasATarget;
        }

        public void SetIsFighting(bool isFighting)
        {
            this.isFighting = isFighting;
        }

        public bool IsFighting()
        {
            return isFighting;
        }

        public void SetIsAttacking(bool isAttacking)
        {
            this.isAttacking = isAttacking;
        }

        public bool IsAttacking()
        {
            return isAttacking;
        }

        public void SetVectorToRival(Vector3 vectorToRival)
        {
            this.vectorToRival = vectorToRival;
            SetDistanceToRival(this.vectorToRival.magnitude);
        }

        public Vector3 GetVectorToRival()
        {
            return vectorToRival;
        }

        public void SetRivalTransform(Transform rivalTransform)
        {
            this.rivalTransform = rivalTransform;
            SetVectorToRival(this.rivalTransform.position - agentTransform.position);
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