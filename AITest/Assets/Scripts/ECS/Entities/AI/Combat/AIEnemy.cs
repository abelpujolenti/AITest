using System;
using System.Collections;
using System.Collections.Generic;
using AI;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using Interfaces.AI.Combat;
using Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECS.Entities.AI.Combat
{
    public class AIEnemy : AICombatAgentEntity<AIEnemyContext>
    {
        [SerializeField] private AIEnemySpecs _aiEnemySpecs;

        [SerializeField] private SphereCollider _originalThreatGroupInfluenceCollider;

        private List<uint> _overlappingEnemies = new List<uint>();

        private AIEnemyContext _enemyContext;

        private ThreatComponent _threatComponent;

        private float _minimumRangeToCastAnAttack;
        private float _maximumRangeToCastAnAttack;
        
        private void Start()
        {
            Setup();
            SetupCombatComponents(_aiEnemySpecs);
            _threatComponent = new ThreatComponent(_aiEnemySpecs.threatLevel);
            
            CalculateMinimumAndMaximumRangeToAttacks();
            
            _enemyContext = new AIEnemyContext(_aiEnemySpecs.totalHealth, GetComponent<CapsuleCollider>().radius, 
                _aiEnemySpecs.sightMaximumDistance, transform, _aiEnemySpecs.threatLevel, _originalThreatGroupInfluenceCollider.radius,
                _aiEnemySpecs.maximumStress, _minimumRangeToCastAnAttack, _maximumRangeToCastAnAttack);
            
            CombatManager.Instance.AddAIEnemy(this, _enemyContext);
            
            StartUpdate();
        }

        protected override IEnumerator UpdateCoroutine()
        {
            while (true)
            {
                UpdateVisibleRivals();

                UpdateVectorToRival();

                if (_enemyContext.IsAttacking())
                {
                    yield return null;
                    continue;
                }
            
                CalculateBestAction();

                yield return null;
            }
        }

        private void CalculateMinimumAndMaximumRangeToAttacks()
        {
            List<AIAttack> attacks = _aiEnemySpecs.aiAttacks;
            
            _minimumRangeToCastAnAttack = attacks[0].minimumRangeCast;
            _maximumRangeToCastAnAttack = attacks[0].maximumRangeCast;

            for (int i = 1; i < attacks.Count; i++)
            {
                AIAttack attack = attacks[i];
                
                float minimumAttackRange = attack.minimumRangeCast;
                float maximumAttackRange = attack.maximumRangeCast;

                if (minimumAttackRange < _minimumRangeToCastAnAttack)
                {
                    _minimumRangeToCastAnAttack = minimumAttackRange;
                }

                if (maximumAttackRange > _maximumRangeToCastAnAttack)
                {
                    _maximumRangeToCastAnAttack = maximumAttackRange;
                }
            }
        }

        public void AddOverlappingEnemyID(uint enemyID)
        {
            _overlappingEnemies.Add(enemyID);
            
            CombatManager.Instance.OnEnemyJoinEnemy(this, enemyID);
        }

        public void RemoveOverlappingEnemy(uint enemyID)
        {
            _overlappingEnemies.Remove(enemyID);
            
            CombatManager.Instance.OnEnemySeparateFromEnemy(this, enemyID);
        }

        public List<uint> GetOverlappingEnemies()
        {
            return _overlappingEnemies;
        }

        public ThreatComponent GetThreatComponent()
        {
            return _threatComponent;
        }

        protected override void UpdateVisibleRivals()
        {
            _visibleRivals = CombatManager.Instance.GetVisibleRivals<AIAlly, AIAllyContext, AIEnemyContext>(this);

            _enemyContext.SetIsSeeingARival(_visibleRivals.Count != 0);
        }

        protected override void CalculateBestAction()
        {
            CombatManager.Instance.CalculateBestAction(this);
        }

        public AttackComponent Attack()
        {
            AttackComponent attackComponent = ReturnNextAttack();
            
            _enemyContext.SetIsAttacking(true);

            return attackComponent;
        }

        private AttackComponent ReturnNextAttack()
        {
            List<AttackComponent> possibleAttacks = new List<AttackComponent>();
            List<float> minimumRangesInsideCurrentRange = new List<float>();
            List<float> maximumRangesInsideCurrentRange = new List<float>();

            float currentMinimumRangeToAttack = _enemyContext.GetMinimumRangeToAttack();
            float currentMaximumRangeToAttack = _enemyContext.GetMaximumRangeToAttack();
            
            foreach (AttackComponent attackComponent in _attackComponents)
            {
                float currentAttackMinimumRangeToCast = attackComponent.GetMinimumRangeCast();
                float currentAttackMaximumRangeToCast = attackComponent.GetMaximumRangeCast();
                
                if (currentAttackMinimumRangeToCast < currentMinimumRangeToAttack ||
                    currentAttackMaximumRangeToCast > currentMaximumRangeToAttack ||
                    attackComponent.IsOnCooldown())
                {
                    continue;
                }
                
                minimumRangesInsideCurrentRange.Add(currentAttackMinimumRangeToCast);
                maximumRangesInsideCurrentRange.Add(currentAttackMaximumRangeToCast);
                
                possibleAttacks.Add(attackComponent);
            }

            int randomNumber = Random.Range(0, possibleAttacks.Count);

            AttackComponent selectedAttackComponent = possibleAttacks[randomNumber];
            
            minimumRangesInsideCurrentRange.RemoveAt(randomNumber);
            maximumRangesInsideCurrentRange.RemoveAt(randomNumber);

            if (minimumRangesInsideCurrentRange.Count == 0)
            {
                _enemyContext.SetMinimumRangeToAttack(_maximumRangeToCastAnAttack);
                _enemyContext.SetMaximumRangeToAttack(_minimumRangeToCastAnAttack);
                return selectedAttackComponent;
            }

            if (Math.Abs(selectedAttackComponent.GetMinimumRangeCast() - currentMinimumRangeToAttack) < 0.3f)
            {
                UpdateMinimumRangeToCast(minimumRangesInsideCurrentRange);
            }

            if (Math.Abs(selectedAttackComponent.GetMaximumRangeCast() - currentMaximumRangeToAttack) < 0.3f)
            {
                UpdateMaximumRangeToCast(maximumRangesInsideCurrentRange);
            }

            return selectedAttackComponent;
        }

        private void UpdateMinimumRangeToCast(List<float> minimumRangesInsideCurrentRange)
        {
            float newMinimumRange = minimumRangesInsideCurrentRange[0];

            for (int i = 1; i < minimumRangesInsideCurrentRange.Count; i++)
            {
                float currentMinimumRange = minimumRangesInsideCurrentRange[i];
                
                if (currentMinimumRange > newMinimumRange)
                {
                    continue;
                }

                newMinimumRange = currentMinimumRange;
            }
            
            _enemyContext.SetMinimumRangeToAttack(newMinimumRange);
        }

        private void UpdateMaximumRangeToCast(List<float> maximumRangesInsideCurrentRange)
        {
            float newMaximumRange = maximumRangesInsideCurrentRange[0];

            for (int i = 1; i < maximumRangesInsideCurrentRange.Count; i++)
            {
                float currentMaximumRange = maximumRangesInsideCurrentRange[i];
                
                if (currentMaximumRange > newMaximumRange)
                {
                    continue;
                }

                newMaximumRange = currentMaximumRange;
            }
            
            _enemyContext.SetMaximumRangeToAttack(newMaximumRange);
        }

        public void OnAttackAvailableAgain(AttackComponent attackComponent)
        {
            float attackMinimumRangeToCast = attackComponent.GetMinimumRangeCast();
            float attackMaximumRangeToCast = attackComponent.GetMaximumRangeCast();

            if (_enemyContext.GetMinimumRangeToAttack() > attackMinimumRangeToCast)
            {
                _enemyContext.SetMinimumRangeToAttack(attackMinimumRangeToCast);
            }

            if (_enemyContext.GetMaximumRangeToAttack() > attackMaximumRangeToCast)
            {
                return;
            }
            
            _enemyContext.SetMaximumRangeToAttack(attackMaximumRangeToCast);
        }

        public override void OnReceiveDamage(DamageComponent damageComponent)
        {
            _enemyContext.SetHealth(_enemyContext.GetHealth() - damageComponent.GetDamage());

            uint combatAgentInstanceID = GetCombatAgentInstance();

            uint health = _enemyContext.GetHealth();

            if (health == 0)
            {
                CombatManager.Instance.OnEnemyDefeated(combatAgentInstanceID);
                return;
            }
            
            //TODO DAMAGE TO STRESS
            _enemyContext.SetCurrentStress(_enemyContext.GetCurrentStress() /**/);
            CombatManager.Instance.OnEnemyReceiveDamage(combatAgentInstanceID, health, _enemyContext.GetCurrentStress());
        }

        public override AIAgentType GetAIAgentType()
        {
            return _aiEnemySpecs.aiAgentType;
        }

        public override AIEnemyContext GetContext()
        {
            return _enemyContext;
        }

        public override void SetLastActionIndex(uint lastActionIndex)
        {
            _enemyContext.SetLastActionIndex(lastActionIndex);
        }

        public override void SetHealth(uint health)
        {
            _enemyContext.SetHealth(health);
        }

        public override void SetRivalIndex(uint rivalIndex)
        {
            _enemyContext.SetRivalIndex(rivalIndex);
        }

        public override void SetRivalRadius(float rivalRadius)
        {
            _enemyContext.SetRivalRadius(rivalRadius);
        }

        public override void SetDistanceToRival(float distanceToRival)
        {
            _enemyContext.SetDistanceToRival(distanceToRival);
        }

        public override void SetIsSeeingARival(bool isSeeingARival)
        {
            _enemyContext.SetIsSeeingARival(isSeeingARival);
        }

        public override void SetHasATarget(bool hasATarget)
        {
            _enemyContext.SetHasATarget(hasATarget);
        }

        public override void SetIsFighting(bool isFighting)
        {
            _enemyContext.SetIsFighting(isFighting);
        }

        public override void SetIsAttacking(bool isAttacking)
        {
            _enemyContext.SetIsAttacking(isAttacking);
        }

        public override void SetVectorToRival(Vector3 vectorToRival)
        {
            _enemyContext.SetVectorToRival(vectorToRival);
        }

        public override void SetRivalTransform(Transform rivalTransform)
        {
            _enemyContext.SetRivalTransform(rivalTransform);
        }

        public void SetCurrentThreatGroup(uint currentThreatGroup)
        {
            _enemyContext.SetCurrentThreatGroup(currentThreatGroup);
        }

        public void SetCurrentStress(float currentStress)
        {
            _enemyContext.SetCurrentStress(currentStress);
        }

        public override IStatWeight GetStatWeightComponent()
        {
            return _threatComponent;
        }

        //CHANGE IT
        private void OnDestroy()
        {
            CombatManager.Instance.OnEnemyDefeated(this);
        }
    }
}
