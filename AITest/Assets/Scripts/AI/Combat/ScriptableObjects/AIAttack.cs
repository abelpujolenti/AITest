using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AI.Combat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AI Attack Properties", menuName = "ScriptableObjects/AI/Combat/AI Attack Properties", order = 0)]
    [Serializable]
    public class AIAttack : ScriptableObject
    {
        public uint totalDamage;

        public float height;

        public bool doesRelativePositionToCasterChange;
        public Vector3 relativePositionToCaster;

        public bool attachToAttacker;

        public bool isRelativePositionXCenterOfColliderX = true;
        public bool isRelativePositionYCenterOfColliderY = true;
        public bool isRelativePositionZCenterOfColliderZ = true;
        
        public float minimumRangeCast;
        public float maximumRangeCast;
        public float timeToCast;
        public bool doesDamageOverTime;
        public float timeDealingDamage;
        public float cooldown;

        public bool itLandsInstantly;

        public Vector3 startRelativePositionToCasterOfTheProjectile;

        public float projectileSpeed;

        public bool doesProjectileExplodeOnAnyContact;
        
        public AIAttackAoE attackAoE;
    }
}