using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public abstract class AICombatAgentSpecs : ScriptableObject
    {
        public uint  totalHealth;

        public float sightMaximumDistance;
        public float damageFeedbackFlashTime;

        public Color damageFeedbackFlashColor;
    }
}