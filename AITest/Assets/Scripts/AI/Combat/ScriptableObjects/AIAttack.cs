using System;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AI Attack Properties", menuName = "ScriptableObjects/AI/Combat/AI Attack Properties", order = 0)]
    [Serializable]
    public class AIAttack : ScriptableObject
    {
        public uint totalDamage;

        public float height;

        public bool doesRelativePositionChange;
        public Vector3 relativePosition;

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

        public float projectileSpeed;

        
        public AIAttackAoE attackAoE;
    }
}