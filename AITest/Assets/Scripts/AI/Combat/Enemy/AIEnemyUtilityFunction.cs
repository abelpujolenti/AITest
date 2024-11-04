using AI.Combat.ScriptableObjects;
using Interfaces.AI.UBS.Enemy;

namespace AI.Combat.Enemy
{
    public static class AIEnemyUtilityFunction
    {
        public static AIEnemyAction GetBestAction(AIEnemyContext context)
        {
            AICombatAgentAction<AIEnemyAction>[] actions = new[]
            {
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.LOOK_FOR_RIVAL),
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.GET_CLOSER_TO_RIVAL),
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.ATTACK),
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.FLEE)
            };

            actions[0].utilityScore = CalculateLookForRivalUtility(context);
            actions[1].utilityScore = CalculateGetCloserToRivalUtility(context);
            actions[2].utilityScore = CalculateAttackUtility(context);
            actions[3].utilityScore = CalculateFleeUtility(context);

            uint index = 0;

            for (uint i = 1; i < actions.Length; i++)
            {
                if (actions[i].utilityScore < actions[index].utilityScore)
                {
                    continue;
                }

                index = i;
            }
            
            return actions[index].GetAIAction();
        }
        
        private static float CalculateLookForRivalUtility(IEnemyLookForRivalUtility enemyLookForRivalUtility)
        {
            return 0;
        }

        private static float CalculateGetCloserToRivalUtility(IEnemyGetCloserToRivalUtility enemyGetCloserToRivalUtility)
        {
            return 0;
        }

        private static float CalculateAttackUtility(IEnemyAttackUtility enemyAttackUtility)
        {
            return 0;
        }
        
        private static float CalculateFleeUtility(IEnemyFleeUtility enemyFleeUtility)  
        {
            return 0;
        }
    }
}