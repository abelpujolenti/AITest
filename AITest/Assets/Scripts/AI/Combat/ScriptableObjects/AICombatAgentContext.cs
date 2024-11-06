using Interfaces.AI.Combat;
using Interfaces.AI.UBS.BaseInterfaces.Get;
using Interfaces.AI.UBS.BaseInterfaces.Property;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public abstract class AICombatAgentContext : IGetTotalHealth, ILastActionIndex, IHealth, IRivalIndex, IGetRadius,
        IRivalRadius, IGetSightMaximumDistance, IDistanceToRival, ISeeingARival, ITarget, IFighting, IAttacking, 
        IVectorToRival, IRivalTransform, IGetAgentTransform, IStatWeight
    {
        private uint _lastActionIndex = 10;
        
        private uint _totalHealth;
        private uint _health;
        private uint _rivalIndex;

        private float _radius;
        private float _rivalRadius;
        private float _sightMaximumDistance;
        private float _distanceToRival;

        private bool _isSeeingARival;
        private bool _hasATarget;
        private bool _isFighting;
        private bool _isAttacking;

        private Vector3 _vectorToRival;

        private Transform _agentTransform;
        private Transform _rivalTransform;

        protected AICombatAgentContext(uint totalHealth, float radius, float sightMaximumDistance, Transform agentTransform)
        {
            _totalHealth = totalHealth;
            _health = totalHealth;
            _radius = radius;
            _sightMaximumDistance = sightMaximumDistance != 0 ? sightMaximumDistance : Mathf.Infinity; 
            _agentTransform = agentTransform;
        }

        public void SetLastActionIndex(uint lastActionIndex)
        {
            _lastActionIndex = lastActionIndex;
        }

        public uint GetLastActionIndex()
        {
            return _lastActionIndex;
        }

        public uint GetTotalHealth()
        {
            return _totalHealth;
        }

        public void SetHealth(uint health)
        {
            _health = health;
        }

        public uint GetHealth()
        {
            return _health;
        }

        public void SetRivalIndex(uint rivalIndex)
        {
            _rivalIndex = rivalIndex;
        }

        public uint GetRivalIndex()
        {
            return _rivalIndex;
        }

        public float GetRadius()
        {
            return _radius;
        }

        public void SetRivalRadius(float rivalRadius)
        {
            _rivalRadius = rivalRadius;
        }

        public float GetRivalRadius()
        {
            return _rivalRadius;
        }

        public float GetSightMaximumDistance()
        {
            return _sightMaximumDistance;
        }

        public void SetDistanceToRival(float distanceToRival)
        {
            _distanceToRival = distanceToRival;
        }

        public float GetDistanceToRival()
        {
            return _distanceToRival;
        }

        public void SetIsSeeingARival(bool isSeeingARival)
        {
            _isSeeingARival = isSeeingARival;
        }

        public bool IsSeeingARival()
        {
            return _isSeeingARival;
        }

        public void SetHasATarget(bool hasATarget)
        {
            _hasATarget = hasATarget;
        }

        public bool HasATarget()
        {
            return _hasATarget;
        }

        public void SetIsFighting(bool isFighting)
        {
            _isFighting = isFighting;
        }

        public bool IsFighting()
        {
            return _isFighting;
        }

        public void SetIsAttacking(bool isAttacking)
        {
            _isAttacking = isAttacking;
        }

        public bool IsAttacking()
        {
            return _isAttacking;
        }

        public void SetVectorToRival(Vector3 vectorToRival)
        {
            _vectorToRival = vectorToRival;
            SetDistanceToRival(_vectorToRival.magnitude - _rivalRadius);
        }

        public Vector3 GetVectorToRival()
        {
            return _vectorToRival;
        }

        public void SetRivalTransform(Transform rivalTransform)
        {
            _rivalTransform = rivalTransform;
            SetVectorToRival(_rivalTransform.position - _agentTransform.position);
        }

        public Transform GetRivalTransform()
        {
            return _rivalTransform;
        }

        public Transform GetAgentTransform()
        {
            return _agentTransform;
        }

        public abstract float GetWeight();
    }
}