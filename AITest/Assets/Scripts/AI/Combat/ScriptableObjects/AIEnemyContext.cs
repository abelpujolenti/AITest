using Interfaces.AI.UBS.Enemy;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIEnemyContext : AICombatAgentContext, IEnemyLookForRivalUtility, IEnemyGetCloserToRivalUtility, 
        IEnemyAttackUtility, IEnemyFleeUtility
    {
        public float threatLevel;

        public AIEnemyContext(uint totalHealth, Transform agentTransform, float threatLevel) : base(totalHealth, agentTransform)
        {
            this.threatLevel = threatLevel;
        }

        public override float GetWeight()
        {
            return threatLevel;
        }
    }
}