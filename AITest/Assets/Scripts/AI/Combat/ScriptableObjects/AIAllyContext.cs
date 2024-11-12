using System.Collections.Generic;
using AI.Combat.Ally;
using Interfaces.AI.UBS.Ally;
using Interfaces.AI.UBS.BaseInterfaces.Property;
using UnityEngine;

namespace AI.Combat.ScriptableObjects
{
    public class AIAllyContext : AICombatAgentContext, IAllyFollowPlayerUtility, IAllyChooseNewRivalUtility,
        IAllyGetCloserToRivalUtility, IAllyAttackUtility, IAllyFleeUtility, IAllyDodgeAttackUtility, 
        IAllyHelpAnotherMoralGroupUtility, IAllyHelpAIAllyUtility, IEnemyStunned
    {
        private uint _oncomingAttackDamage;
        private uint _enemyHealth;

        private float _remainingDistance;
        private float _stoppingDistance;
        private float _height;
        private float _moralWeight;
        private float _radiusOfAlert;
        private float _threatWeightOfTarget = 0;
        private float _enemyMaximumStress;
        private float _enemyCurrentStress;

        private bool _canDefeatEnemy;
        private bool _canStunEnemy;
        private bool _isEnemyStunned;
        private bool _isUnderThreat;
        private bool _isUnderAttack;
        private bool _isAnotherMoralGroupUnderThreat;
        private bool _isAirborne;
        private bool _wasRetreatOrderUsed;
        private bool _wasAttackOrderUsed;
        private bool _wasFleeOrderUsed;

        private List<float> _distancesToThreatGroupsThatThreatMe = new List<float>();
        private Dictionary<uint, float> _groupsPriorityToHelp = new Dictionary<uint, float>();

        public AIAllyContext(uint totalHealth, uint currentGroup, float radius, float sightMaximumDistance, 
            float minimumRangeToAttack, float maximumRangeToAttack, Transform agentTransform, float stoppingDistance, 
            float height, float moralWeight, float radiusOfAlert) : base(totalHealth, currentGroup, radius, 
            sightMaximumDistance, minimumRangeToAttack, maximumRangeToAttack, agentTransform)
        {
            _repeatableActions.Add((uint)AIAllyAction.CHOOSE_NEW_RIVAL);
            _repeatableActions.Add((uint)AIAllyAction.ROTATE);
            _repeatableActions.Add((uint)AIAllyAction.DODGE_ATTACK);
            _repeatableActions.Add((uint)AIAllyAction.ATTACK);

            _stoppingDistance = stoppingDistance;
            _height = height;
            _moralWeight = moralWeight;
            _radiusOfAlert = radiusOfAlert;
        }

        public void SetRivalHealth(uint rivalHealth)
        {
            _enemyHealth = rivalHealth;
        }

        public uint GetRivalHealth()
        {
            return _enemyHealth;
        }

        public void SetStoppingDistance(float stoppingDistance)
        {
            _stoppingDistance = stoppingDistance;
        }

        public void SetRemainingDistance(float remainingDistance)
        {
            _remainingDistance = remainingDistance;
        }

        public float GetRemainingDistance()
        {
            return _remainingDistance;
        }

        public float GetStoppingDistance()
        {
            return _stoppingDistance;
        }

        public float GetHeight()
        {
            return _height;
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

        public void SetCanDefeatEnemy(bool canDefeatEnemy)
        {
            _canDefeatEnemy = canDefeatEnemy;
        }

        public bool CanDefeatEnemy()
        {
            return _canDefeatEnemy;
        }

        public void SetCanStunEnemy(bool canStunEnemy)
        {
            _canStunEnemy = canStunEnemy;
        }

        public bool CanStunEnemy()
        {
            return _canStunEnemy;
        }

        public void SetIsEnemyStunned(bool isEnemyStunned)
        {
            _isEnemyStunned = isEnemyStunned;
        }

        public bool IsEnemyStunned()
        {
            return _isEnemyStunned;
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

        public void SetIsAnotherMoralGroupUnderThreat(bool isAnotherMoralGroupUnderThreat)
        {
            _isAnotherMoralGroupUnderThreat = isAnotherMoralGroupUnderThreat;
        }

        public bool IsAnotherMoralGroupUnderThreat()
        {
            return _isAnotherMoralGroupUnderThreat;
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

        public void AddGroupToHelp(uint groupID)
        {
            _groupsPriorityToHelp.Add(groupID, 0);
        }

        public void RemoveGroupToHelp(uint groupID)
        {
            _groupsPriorityToHelp.Remove(groupID);
        }

        public void SetGroupHelpPriority(uint groupID, float helpPriority)
        {
            _groupsPriorityToHelp[groupID] = helpPriority;
        }

        public Dictionary<uint, float> GetGroupsHelpPriority()
        {
            return _groupsPriorityToHelp;
        }

        public override float GetWeight()
        {
            return _moralWeight;
        }
    }
}