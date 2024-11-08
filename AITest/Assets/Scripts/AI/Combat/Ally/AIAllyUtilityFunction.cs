﻿using System;
using AI.Combat.ScriptableObjects;
using Interfaces.AI.UBS.Ally;
using UnityEngine;

namespace AI.Combat.Ally
{
    public static class AIAllyUtilityFunction
    {
        public static AIAllyAction GetBestAction(AIAllyContext context)
        {
            AICombatAgentAction<AIAllyAction>[] actions = new[]
            {
                new AICombatAgentAction<AIAllyAction>(AIAllyAction.FOLLOW_PLAYER),
                new AICombatAgentAction<AIAllyAction>(AIAllyAction.LOOK_FOR_RIVAL),
                new AICombatAgentAction<AIAllyAction>(AIAllyAction.GET_CLOSER_TO_RIVAL),
                new AICombatAgentAction<AIAllyAction>(AIAllyAction.ATTACK),
                new AICombatAgentAction<AIAllyAction>(AIAllyAction.FLEE),
                new AICombatAgentAction<AIAllyAction>(AIAllyAction.DODGE_ATTACK),
                new AICombatAgentAction<AIAllyAction>(AIAllyAction.HELP_ALLY)
            };

            actions[0].utilityScore = CalculateFollowPlayerUtility(context);
            actions[1].utilityScore = CalculateLookForRivalUtility(context);
            actions[2].utilityScore = CalculateGetCloserToRivalUtility(context);
            actions[3].utilityScore = CalculateAttackUtility(context);
            actions[4].utilityScore = CalculateFleeUtility(context);
            actions[5].utilityScore = CalculateDodgeAttackUtility(context);
            actions[6].utilityScore = CalculateHelpAllyUtility(context);

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

        private static float CalculateFollowPlayerUtility(IAllyFollowPlayerUtility allyFollowPlayerUtility)
        {
            return Convert.ToInt16(allyFollowPlayerUtility.IsInRetreatState() || !allyFollowPlayerUtility.IsSeeingARival());
        }
        
        //RETHINK
        private static float CalculateLookForRivalUtility(IAllyLookForRivalUtility allyLookForRivalUtility)
        {
            //THINGS TO TAKE CARE
            
            //START | 1 -> IS_FIGHTING
            
            //1 FALSE | RETURN 0.5
            //1 TRUE | 2 -> IS_SEEING_AN_ENEMY
            
            //2 TRUE | 2.1 -> MORAL < THREAT_WEIGHT_OF_TARGET
            //2 FALSE | 0
            
            //2.1 TRUE | RETURN 0.6
            //2.1 FALSE | RETURN 0 
            
            if (!allyLookForRivalUtility.IsFighting())
            {
                return 0.7f;
            }
            
            if (allyLookForRivalUtility.IsSeeingARival())
            {
                if (allyLookForRivalUtility.GetMoralWeight() < allyLookForRivalUtility.GetThreatWeightOfTarget())
                {
                    return 0.6f;
                }
            }
            
            return 0;
        }

        private static float CalculateGetCloserToRivalUtility(IAllyGetCloserToRivalUtility allyGetCloserToRivalUtility)
        {
            if (allyGetCloserToRivalUtility.GetBasicAttackMaximumRange() > allyGetCloserToRivalUtility.GetDistanceToEnemy())
            {
                return 0.5f;
            }
            
            return 0;
        }

        private static float CalculateAttackUtility(IAllyAttackUtility allyAttackUtility)
        {
            //THINGS TO TAKE CARE
            
            //START | 1 -> BASIC_ATTACK_RANGE < DISTANCE_TO_ENEMY
            
            //1 TRUE | 2 -> IS_IN_ATTACK_STATE
            //1 FALSE | RETURN 0
            
            //2 TRUE | RETURN 1
            //2 FALSE | 3 -> BASIC_ATTACK_DAMAGE >= RIVAL_HEALTH
            
            //3 TRUE | RETURN 0.9
            //3 FALSE | 4 -> BASIC_STUN_DAMAGE >= RIVAL_STRESS_REMAINING_TO_STUN
            
            //4 TRUE | RETURN 0.5
            //4 FALSE | RETURN 0.4

            if (!allyAttackUtility.HasATarget())
            {
                return 0;
            }

            Vector3 ownPosition = allyAttackUtility.GetAgentTransform().position;
            Vector3 rivalPosition = allyAttackUtility.GetRivalTransform().position;

            float distanceToEnemy = (rivalPosition - ownPosition).magnitude;

            if (allyAttackUtility.GetBasicAttackMaximumRange() < distanceToEnemy &&
                Vector3.Angle(allyAttackUtility.GetAgentTransform().forward, allyAttackUtility.GetRivalTransform().position) < 15f)
            {
                return 0;
            }
            
            if (allyAttackUtility.IsInAttackState())
            {
                return 1;
            }

            if (allyAttackUtility.GetBasicAttackDamage() >= allyAttackUtility.GetRivalHealth())
            {
                return 0.9f;
            }

            if (allyAttackUtility.GetBasicStressDamage() >= allyAttackUtility.GetRivalStressRemainingToStun())
            {
                return 0.5f;
            }
            
            return 0.4f;
        }
        
        private static float CalculateFleeUtility(IAllyFleeUtility allyFleeUtility)  
        {
            //THINGS TO TAKE CARE
            
            //START | 1 -> IS_IN_FLEE_STATE
            
            //1 TRUE | RETURN 1
            //1 FALSE | 2 IS_UNDER_THREAT
            
            //2 TRUE | RETURN 0.8
            //2 FALSE | RETURN 0

            if (!allyFleeUtility.IsSeeingARival())
            {
                return 0;
            }
            
            if (allyFleeUtility.IsInFleeState())
            {
                return 1;
            }

            if (allyFleeUtility.IsUnderThreat())
            {
                return 0.8f;
            }
            
            return 0;
        }
        
        private static float CalculateDodgeAttackUtility(IAllyDodgeAttackUtility allyDodgeAttackUtility)
        {
            //THINGS TO TAKE CARE
            
            //START | 1 -> IS_UNDER_ATTACK
            
            //1 TRUE | 1.1 
            //1 FALSE | RETURN 0
            
            //2 TRUE | RETURN 0.8
            //2 FALSE | RETURN 0
            
            if (!allyDodgeAttackUtility.IsUnderAttack())
            {
                return 0;
            }

            if (allyDodgeAttackUtility.GetHealth() < allyDodgeAttackUtility.GetTotalHealth() * 0.3f)
            {
                return 0.8f;
            }

            return 0.8f;
        }
        
        private static float CalculateHelpAllyUtility(IAllyHelpAllyUtility allyHelpAllyUtility)
        {
            if (!allyHelpAllyUtility.IsFighting() && allyHelpAllyUtility.IsAnotherAllyUnderThreat())
            {
                return 1;
            }
            
            return 0;
        }
    }
}