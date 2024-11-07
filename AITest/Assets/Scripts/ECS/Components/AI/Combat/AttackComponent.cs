using AI.Combat;
using AI.Combat.ScriptableObjects;
using UnityEngine;

namespace ECS.Components.AI.Combat
{
    public abstract class AttackComponent
    {
        private uint _totalDamage;
        private float _height;
        private bool _doesRelativePositionToCasterChange;
        private Vector3 _relativePositionToCaster;
        private bool _isAttachedToAttacker;
        private float _minimumRangeToCast;
        private float _maximumRangeToCast;
        private bool _isRelativePositionXCenterOfColliderX;
        private bool _isRelativePositionYCenterOfColliderY;
        private bool _isRelativePositionZCenterOfColliderZ;
        private float _timeToCast;
        private float _currentTimeToFinishCast;
        private float _cooldown;
        private float _currentCooldown = 0;

        private bool _itLandsInstantly;

        private Vector3 _startRelativePositionToCasterOfTheProjectile;

        private float _projectileSpeed;

        private bool _doesProjectileExplodeOnAnyContact;

        private bool _doesDamageOverTime;

        private float _timeDealingDamage;
        private float _remainingTimeDealingDamage;

        private AIAttackAoEType _aiAttackAoEType;

        protected AttackComponent(AIAttack aiAttack)
        {
            _totalDamage = aiAttack.totalDamage;
            _height = aiAttack.height;
            _doesRelativePositionToCasterChange = aiAttack.doesRelativePositionToCasterChange;
            _relativePositionToCaster = aiAttack.relativePositionToCaster;
            _isAttachedToAttacker = aiAttack.attachToAttacker;
            _minimumRangeToCast = aiAttack.minimumRangeCast;
            _maximumRangeToCast = aiAttack.maximumRangeCast;
            _isRelativePositionXCenterOfColliderX = aiAttack.isRelativePositionXCenterOfColliderX;
            _isRelativePositionYCenterOfColliderY = aiAttack.isRelativePositionYCenterOfColliderY;
            _isRelativePositionZCenterOfColliderZ = aiAttack.isRelativePositionZCenterOfColliderZ;
            _timeToCast = aiAttack.timeToCast;
            _cooldown = aiAttack.cooldown;
            _itLandsInstantly = aiAttack.itLandsInstantly;
            _startRelativePositionToCasterOfTheProjectile = aiAttack.startRelativePositionToCasterOfTheProjectile;
            _projectileSpeed = aiAttack.projectileSpeed;
            _doesProjectileExplodeOnAnyContact = aiAttack.doesProjectileExplodeOnAnyContact;
            _doesDamageOverTime = aiAttack.doesDamageOverTime;
            _timeDealingDamage = aiAttack.timeDealingDamage;
            _aiAttackAoEType = aiAttack.attackAoE.aiAttackAoEType;
        }

        public uint GetDamage()
        {
            return _totalDamage;
        }

        public float GetHeight()
        {
            return _height;
        }

        public Vector3 GetRelativePosition()
        {
            return _relativePositionToCaster;
        }

        public void SetRelativePositionIfPossible(Vector3 position)
        {
            if (!_doesRelativePositionToCasterChange)
            {
                return; 
            }   
            
            _relativePositionToCaster = position;
        }

        public bool IsAttachedToAttacker()
        {
            return _isAttachedToAttacker;
        }

        public float GetMinimumRangeCast()
        {
            return _minimumRangeToCast;
        }

        public float GetMaximumRangeCast()
        {
            return _maximumRangeToCast;
        }

        public bool IsRelativePositionXCenterOfColliderX()
        {
            return _isRelativePositionXCenterOfColliderX;
        }

        public bool IsRelativePositionYCenterOfColliderY()
        {
            return _isRelativePositionYCenterOfColliderY;
        }

        public bool IsRelativePositionZCenterOfColliderZ()
        {
            return _isRelativePositionZCenterOfColliderZ;
        }

        public void StartCastTime()
        {
            _currentTimeToFinishCast = _timeToCast;
        }

        public void DecreaseCurrentCastTime()
        {
            _currentTimeToFinishCast =
                Mathf.Max(0, _currentTimeToFinishCast - Time.deltaTime);
        }

        public bool IsCasting()
        {
            return _currentTimeToFinishCast != 0;
        }

        public float GetCurrentTimeToFinishCast()
        {
            return _currentTimeToFinishCast;
        }

        public void StartCooldown()
        {
            _currentCooldown = _cooldown;
        }

        public void DecreaseCooldown()
        {
            _currentCooldown = Mathf.Max(0, _currentCooldown - Time.deltaTime);
        }

        public float GetCurrentCooldown()
        {
            return _currentCooldown;
        }

        public bool IsOnCooldown()
        {
            return _currentCooldown != 0;
        }

        public bool ItLandsInstantly()
        {
            return _itLandsInstantly;
        }

        public Vector3 GetStartRelativePositionToCasterOfTheProjectile()
        {
            return _startRelativePositionToCasterOfTheProjectile;
        }

        public float GetProjectileSpeed()
        {
            return _projectileSpeed;
        }

        public bool DoesProjectileExplodeOnAnyContact()
        {
            return _doesProjectileExplodeOnAnyContact;
        }

        public bool DoesDamageOverTime()
        {
            return _doesDamageOverTime;
        }

        public void StartDamageOverTime()
        {
            _remainingTimeDealingDamage = _timeDealingDamage;
        }

        public void DecreaseRemainingTimeDealingDamage()
        {
            _remainingTimeDealingDamage = Mathf.Max(0, _remainingTimeDealingDamage - Time.deltaTime);
        }

        public bool DidDamageOverTimeFinished()
        {
            return _remainingTimeDealingDamage != 0;
        }

        public AIAttackAoEType GetAIAttackAoEType()
        {
            return _aiAttackAoEType;
        }
    }
}