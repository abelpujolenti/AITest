using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AI Enemy Properties", menuName = "ScriptableObjects/AI/Combat/Entity/AI Enemy Properties", order = 1)]
    public class AIEnemySpecs : AICombatAgentSpecs
    {
        public readonly AIAgentType aiAgentType = AIAgentType.ENEMY;

        public float threatLevel;
        public float maximumStress;
    }
}