using System.Collections.Generic;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AI Ally Properties", menuName = "ScriptableObjects/AI/Combat/Entity/AI Ally Properties", order = 0)]
    public class AIAllySpecs : AICombatAgentSpecs
    {
        public readonly AIAgentType aiAgentType = AIAgentType.ALLY;
        
        public float moralWeight;
        public float radiusOfAlert;
        public readonly float faintDuration = 10;

        [SerializeField] public List<AIAllyAttack> aiAttacks;
    }
}