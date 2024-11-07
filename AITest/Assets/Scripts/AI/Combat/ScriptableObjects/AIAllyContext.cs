using System.Collections.Generic;
using AI.Combat.Ally;
using Interfaces.AI.UBS.Ally;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIAllyContext : AICombatAgentContext, IAllyFollowPlayerUtility, IAllyChooseNewRivalUtility,
        IAllyGetCloserToRivalUtility, IAllyAttackUtility, IAllyFleeUtility, IAllyDodgeAttackUtility, IAllyHelpAllyUtility
    {
        private uint _basicAttackDamage;
        private uint _oncomingAttackDamage;
        private uint _enemyHealth;
        private uint _threatGroupOfTarget = 0;
        
        private float _basicStressDamage;
        private float _basicAttackMaximumRange;
        private float _moralWeight;
        private float _radiusOfAlert;
        private float _threatWeightOfTarget = 0;
        private float _enemyMaximumStress;
        private float _enemyCurrentStress;
        
        private bool _isUnderThreat;
        private bool _isUnderAttack;
        private bool _isAnotherAllyUnderThreat;
        private bool _isAirborne;
        private bool _wasRetreatOrderUsed;
        private bool _wasAttackOrderUsed;
        private bool _wasFleeOrderUsed;

        private List<float> _distancesToThreatGroupsThatThreatMe;
        private uint[] _threatGroupsThatFightAllies;

        public AIAllyContext(uint totalHealth, float radius, float sightMaximumDistance, Transform agentTransform, 
            float basicAttackMaximumRange, uint basicAttackDamage, float basicStressDamage, float moralWeight, 
            float radiusOfAlert) : 
            base(totalHealth, radius, sightMaximumDistance, agentTransform)
        {
            _repeatableActions.Add((uint)AIAllyAction.CHOOSE_NEW_RIVAL);
            _repeatableActions.Add((uint)AIAllyAction.ATTACK);
            
            _basicAttackMaximumRange = basicAttackMaximumRange;
            _basicAttackDamage = basicAttackDamage;
            _basicStressDamage = basicStressDamage;
            _moralWeight = moralWeight;
            _radiusOfAlert = radiusOfAlert;
        }

        public uint GetBasicAttackDamage()
        {
            return _basicAttackDamage;
        }

        public void SetRivalHealth(uint rivalHealth)
        {
            _enemyHealth = rivalHealth;
        }

        public uint GetRivalHealth()
        {
            return _enemyHealth;
        }

        public void SetThreatGroupOfTarget(uint threatGroupOfTarget)
        {
            _threatGroupOfTarget = threatGroupOfTarget;
        }

        public uint GetThreatGroupOfTarget()
        {
            return _threatGroupOfTarget;
        }

        public float GetBasicStressDamage()
        {
            return _basicStressDamage;
        }

        public float GetBasicAttackMaximumRange()
        {
            return _basicAttackMaximumRange;
        }

        public void SetMoralWeight(float moralWeight)
        {
            _moralWeight = moralWeight;
        }

        public float GetMoralWeight()
        {
            return _moralWeight;
        }

        public float GetRadiusOfAlert()
        {
            return _radiusOfAlert;
        }

        public void SetThreatWeightOfTarget(float threatWeightOfTarget)
        {
            _threatWeightOfTarget = threatWeightOfTarget;
        }

        public float GetThreatWeightOfTarget()
        {
            return _threatWeightOfTarget;
        }

        public void SetRivalMaximumStress(float rivalMaximumStress)
        {
            _enemyMaximumStress = rivalMaximumStress;
        }

        public float GetRivalMaximumStress()
        {
            return _enemyMaximumStress;
        }

        public void SetRivalCurrentStress(float rivalCurrentStress)
        {
            _enemyCurrentStress = rivalCurrentStress;
        }

        public float GetRivalCurrentStress()
        {
            return _enemyCurrentStress;
        }

        public void SetIsUnderThreat(bool isUnderThreat)
        {
            _isUnderThreat = isUnderThreat;
        }

        public bool IsUnderThreat()
        {
            return _isUnderThreat;
        }

        public void SetIsUnderAttack(bool isUnderAttack)
        {
            _isUnderAttack = isUnderAttack;
        }

        public bool IsUnderAttack()
        {
            return _isUnderAttack;
        }

        public void SetOncomingAttackDamage(uint oncomingAttackDamage)
        {
            _oncomingAttackDamage = oncomingAttackDamage;
        }

        public uint GetOncomingAttackDamage()
        {
            return _oncomingAttackDamage;
        }

        public void SetIsAnotherAllyUnderThreat(bool isAnotherAllyUnderThreat)
        {
            _isAnotherAllyUnderThreat = isAnotherAllyUnderThreat;
        }

        public bool IsAnotherAllyUnderThreat()
        {
            return _isAnotherAllyUnderThreat;
        }

        public void SetIsAirborne(bool isAirborne)
        {
            _isAirborne = isAirborne;
        }

        public bool IsAirborne()
        {
            return _isAirborne;
        }

        public void SetIsInRetreatState(bool isInRetreatState)
        {
            _wasRetreatOrderUsed = isInRetreatState;
        }

        public bool IsInRetreatState()
        {
            return _wasRetreatOrderUsed;
        }

        public void SetIsInAttackState(bool isInAttackState)
        {
            _wasAttackOrderUsed = isInAttackState;
        }

        public bool IsInAttackState()
        {
            return _wasAttackOrderUsed;
        }

        public void SetIsInFleeState(bool isInFleeState)
        {
            _wasFleeOrderUsed = isInFleeState;
        }

        public bool IsInFleeState()
        {
            return _wasFleeOrderUsed;
        }

        public void SetDistancesToThreatGroupsThatThreatMe(List<float> distancesToThreatGroupsThatThreatMe)
        {
            _distancesToThreatGroupsThatThreatMe = distancesToThreatGroupsThatThreatMe;
        }

        public List<float> GetDistancesToThreatGroupsThatThreatMe()
        {
            return _distancesToThreatGroupsThatThreatMe;
        }

        public void SetThreatGroupsThatFightAllies(uint[] threatGroupsThatFightAllies)
        {
            _threatGroupsThatFightAllies = threatGroupsThatFightAllies;
        }

        public uint[] GetThreatGroupsThatFightAllies()
        {
            return _threatGroupsThatFightAllies;
        }

        public override float GetWeight()
        {
            return _moralWeight;
        }
    }
}