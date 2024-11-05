using System;
using AI.Combat.ScriptableObjects;
using Interfaces.AI.UBS.BaseInterfaces;
using Interfaces.AI.UBS.Enemy;
using UnityEngine;

namespace AI.Combat.Enemy
{
    public class AIEnemyUtilityFunction : IGetBestAction<AIEnemyAction, AIEnemyContext>
    {
        public AIEnemyAction GetBestAction(AIEnemyContext context)
        {
            AICombatAgentAction<AIEnemyAction>[] actions = new[]
            {
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.PATROL),
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.CHOOSE_NEW_RIVAL),
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.GET_CLOSER_TO_RIVAL),
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.ATTACK),
                new AICombatAgentAction<AIEnemyAction>(AIEnemyAction.FLEE)
            };

            actions[0].utilityScore = CalculatePatrolUtility(context);
            actions[1].utilityScore = CalculateLookForRivalUtility(context);
            actions[2].utilityScore = CalculateGetCloserToRivalUtility(context);
            actions[3].utilityScore = CalculateAttackUtility(context);
            actions[4].utilityScore = CalculateFleeUtility(context);

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

        private static float CalculatePatrolUtility(IEnemyPatrolUtility enemyPatrolUtility)
        {
            return Convert.ToInt16(!enemyPatrolUtility.IsSeeingARival());
        }

        private static float CalculateLookForRivalUtility(IEnemyChooseNewRivalUtility enemyChooseNewRivalUtility)
        {
            if (!enemyChooseNewRivalUtility.IsSeeingARival())
            {
                return 0;
            }
            
            if (!enemyChooseNewRivalUtility.HasATarget())
            {
                return 1f;
            }
            
            return 0;
        }
        
        private static float CalculateGetCloserToRivalUtility(IEnemyGetCloserToRivalUtility enemyGetCloserToRivalUtility)
        {
            if (!enemyGetCloserToRivalUtility.HasATarget() || enemyGetCloserToRivalUtility.IsAttacking())
            {
                return 0;
            }
            
            return 1;
        }

        private static float CalculateAttackUtility(IEnemyAttackUtility enemyAttackUtility)
        {
            if (!enemyAttackUtility.HasATarget())
            {
                return 0;
            }

            if (/*enemyAttackUtility.GetBasicAttackMaximumRange() < distanceToEnemy &&*/
                Vector3.Angle(enemyAttackUtility.GetAgentTransform().forward, enemyAttackUtility.GetVectorToRival()) < 15f)
            {
                return 0.9f;
            }
            
            return 0;
        }
        
        private static float CalculateFleeUtility(IEnemyFleeUtility enemyFleeUtility)  
        {
            return 0;
        }
    }
}