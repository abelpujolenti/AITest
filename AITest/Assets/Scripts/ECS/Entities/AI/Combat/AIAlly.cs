using AI;
using AI.Combat.Ally;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using Interfaces.AI.Combat;
using Managers;
using UnityEngine;

namespace ECS.Entities.AI.Combat
{
    public class AIAlly : AICombatAgentEntity<AIAllyContext>
    {
        [SerializeField] private AIAllySpecs _aiAllySpecs;

        private AIAllyContext _allyContext;
        
        private MoralComponent _moralComponent;
        
        private DieComponent _dieComponent;

        private void Start()
        {
            Setup();
            SetupCombatComponents(_aiAllySpecs);
            _moralComponent = new MoralComponent(_aiAllySpecs.moralWeight);
            _dieComponent = new DieComponent();
            _allyContext = new AIAllyContext(_aiAllySpecs.totalHealth, transform, _aiAllySpecs.aiAttacks[0].maximumRangeCast,
                _aiAllySpecs.aiAttacks[0].totalDamage, _aiAllySpecs.basicStressDamage, _aiAllySpecs.moralWeight);
            
            CombatManager.Instance.AddAIAlly(this, _allyContext);
        }

        public ref MoralComponent GetMoralComponent()
        {
            return ref _moralComponent;
        }
        
        public DieComponent GetDieComponent()
        {
            return _dieComponent;
        }

        protected override void UpdateVisibleRivals()
        {
            _visibleRivals = CombatManager.Instance.GetVisibleRivals<AIEnemy, AIEnemyContext, AIAllyContext>(this);

            _allyContext.SetIsSeeingARival(_visibleRivals.Count != 0);
        }
        
        protected override void CalculateBestAction()
        {
            CombatManager.Instance.CalculateBestAction(this);
        }

        public override void Attack(uint attackIndex)
        {
            //TODO
            _allyContext.SetIsAttacking(true);
        }

        public override void OnReceiveDamage(DamageComponent damageComponent)
        {
            _allyContext.SetHealth(_allyContext.GetHealth() - damageComponent.GetDamage());

            if (_allyContext.GetHealth() != 0) 
            {
                return;
            }
            
            CombatManager.Instance.OnAllyDefeated(GetCombatAgentInstance());
        }

        public override AIAgentType GetAIAgentType()
        {
            return _aiAllySpecs.aiAgentType;
        }

        public override AIAllyContext GetContext()
        {
            return _allyContext;
        }

        public override void SetLastActionIndex(uint lastActionIndex)
        {
            _allyContext.SetLastActionIndex(lastActionIndex);
        }

        public override void SetHealth(uint health)
        {
            _allyContext.SetHealth(health);
        }

        public override void SetRivalIndex(uint rivalIndex)
        {
            _allyContext.SetRivalIndex(rivalIndex);
        }

        public override void SetDistanceToRival(float distanceToRival)
        {
            _allyContext.SetDistanceToRival(distanceToRival);
        }

        public override void SetIsSeeingARival(bool isSeeingARival)
        {
            _allyContext.SetIsSeeingARival(isSeeingARival);
        }

        public override void SetHasATarget(bool hasATarget)
        {
            _allyContext.SetHasATarget(hasATarget);
        }

        public override void SetIsFighting(bool isFighting)
        {
            _allyContext.SetIsFighting(isFighting);
        }

        public override void SetIsAttacking(bool isAttacking)
        {
            _allyContext.SetIsAttacking(isAttacking);
        }

        public override void SetVectorToRival(Vector3 vectorToRival)
        {
            _allyContext.SetVectorToRival(vectorToRival);
        }

        public override void SetRivalTransform(Transform rivalTransform)
        {
            _allyContext.SetRivalTransform(rivalTransform);
        }

        public override IStatWeight GetStatWeightComponent()
        {
            return _moralComponent;
        }

        public void SetOncomingAttackDamage(uint oncomingAttackDamage)
        {
            _allyContext.SetOncomingAttackDamage(oncomingAttackDamage);
        }

        public void SetEnemyHealth(uint enemyHealth)
        {
            _allyContext.SetRivalHealth(enemyHealth);
        }

        public void SetThreatGroupOfTarget(uint threatGroupOfTarget)
        {
            _allyContext.SetThreatGroupOfTarget(threatGroupOfTarget);
        }

        public void SetMoralWeight(float moralWeight)
        {
            _allyContext.SetMoralWeight(moralWeight);
        }

        public void SetThreatWeightOfTarget(float threatWeightOfTarget)
        {
            _allyContext.SetThreatWeightOfTarget(threatWeightOfTarget);
        }

        public void SetEnemyMaximumStress(float enemyMaximumStress)
        {
            _allyContext.SetRivalMaximumStress(enemyMaximumStress);
        }

        public void SetEnemyCurrentStress(float enemyCurrentStress)
        {
            _allyContext.SetRivalCurrentStress(enemyCurrentStress);
        }

        public void SetIsUnderThreat(bool isUnderThreat)
        {
            _allyContext.SetIsUnderThreat(isUnderThreat);
        }

        public void SetIsUnderAttack(bool isUnderAttack)
        {
            _allyContext.SetIsUnderAttack(isUnderAttack);
        }

        public void SetIsAnotherAllyUnderThreat(bool isAnotherAllyUnderThreat)
        {
            _allyContext.SetIsAnotherAllyUnderThreat(isAnotherAllyUnderThreat);
        }

        public void SetIsAirborne(bool isAirborne)
        {
            _allyContext.SetIsAirborne(isAirborne);
        }

        public void SetState(AIAllyOrders allyOrder)
        {
            _allyContext.SetIsInRetreatState(allyOrder == AIAllyOrders.RETREAT);
            _allyContext.SetIsInAttackState(allyOrder == AIAllyOrders.ATTACK);
            _allyContext.SetIsInFleeState(allyOrder == AIAllyOrders.FLEE);
        }
    }
}