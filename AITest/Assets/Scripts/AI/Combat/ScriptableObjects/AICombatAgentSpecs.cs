using System.Collections.Generic;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public abstract class AICombatAgentSpecs : ScriptableObject
    {
        public uint  totalHealth;

        public float sightMaximumDistance;
        public float flashTime;

        public Color flashColor;

        [SerializeField] public List<AIAttack> aiAttacks;
    }
}