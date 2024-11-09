using System;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AI Ally Attack Properties", menuName = "ScriptableObjects/AI/Combat/AI Ally Attack Properties", order = 1)]
    [Serializable]
    public class AIAllyAttack : AIAttack
    {
        public float stressDamage;
    }
}