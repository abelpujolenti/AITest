using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AI Ally Properties", menuName = "ScriptableObjects/AI/Combat/Entity/AI Ally Properties", order = 0)]
    public class AIAllySpecs : AICombatAgentSpecs
    {
        public readonly AIAgentType aiAgentType = AIAgentType.ALLY;
        
        public float basicStressDamage;
        public float moralWeight;
    }
}