using System;
using System.Collections;
using System.Collections.Generic;
using AI;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using ECS.Entities.AI.Navigation;
using Interfaces.AI.Combat;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECS.Entities.AI.Combat
{
    public abstract class AICombatAgentEntity<TContext, TAttackComponent, TDamageComponent> : NavMeshAgentEntity 
        where TContext : AICombatAgentContext
        where TAttackComponent : AttackComponent
        where TDamageComponent : DamageComponent
    {
        private uint _combatAgentInstanceID;

        protected TContext _context;

        //BIG REFACTOR, I HATE DUPLICATE CLASSES
        protected List<TAttackComponent> _attackComponents = new List<TAttackComponent>();
        //

        protected List<uint> _visibleRivals = new List<uint>();

        protected DamageFeedbackComponent _damageFeedbackComponent;

        protected DefeatComponent _defeatComponent;

        protected IGroup _groupComponent;

        private Coroutine _updateCoroutine;

        protected float _minimumRangeToCastAnAttack;
        protected float _maximumRangeToCastAnAttack;
        
        protected void StartUpdate()
        {
            if (_updateCoroutine != null)
            {
                return;
            }
            _updateCoroutine = StartCoroutine(UpdateCoroutine());
        }

        protected void StopUpdate()
        {
            StopCoroutine(_updateCoroutine);
            _updateCoroutine = null;
        }

        protected override IEnumerator RotateToNextPathCornerCoroutine()
        {
            Vector3 vectorToNextPathCorner;
            do
            {
                Transform ownTransform = transform;
                
                vectorToNextPathCorner = _navMeshAgent.path.corners[1] - ownTransform.position;
                vectorToNextPathCorner.y = 0;
                
                Quaternion rotation = Quaternion.LookRotation(vectorToNextPathCorner);
                transform.rotation = Quaternion.Lerp(ownTransform.rotation, rotation, _rotationSpeed * Time.deltaTime);
                yield return null;
                
            } while (Vector3.Angle(transform.forward, vectorToNextPathCorner) >= 30f);

            _isRotating = false;
            
            GetContext().SetIsAttacking(false);
            
            ContinueNavigation();
        }

        protected abstract IEnumerator UpdateCoroutine();

        protected void SetupCombatComponents(AICombatAgentSpecs aiCombatAgentSpecs)
        {
            _combatAgentInstanceID = (uint)gameObject.GetInstanceID();
            
            _damageFeedbackComponent = new DamageFeedbackComponent(GetComponent<MeshRenderer>(), 
                aiCombatAgentSpecs.flashTime, aiCombatAgentSpecs.flashColor);
        }

        protected void CalculateMinimumAndMaximumRangeToAttacks(List<TAttackComponent> attacks)
        {
            _minimumRangeToCastAnAttack = attacks[0].GetMinimumRangeCast();
            _maximumRangeToCastAnAttack = attacks[0].GetMaximumRangeCast();

            for (int i = 1; i < attacks.Count; i++)
            {
                TAttackComponent attack = attacks[i];
                
                float minimumAttackRange = attack.GetMinimumRangeCast();
                float maximumAttackRange = attack.GetMaximumRangeCast();

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

        protected TAttackComponent ReturnNextAttack()
        {
            List<TAttackComponent> possibleAttacks = new List<TAttackComponent>();
            
            List<float> minimumRangesInsideCurrentRange = new List<float>();
            List<float> maximumRangesInsideCurrentRange = new List<float>();

            float currentMinimumRangeToAttack = _context.GetMinimumRangeToAttack();
            float currentMaximumRangeToAttack = _context.GetMaximumRangeToAttack();
            
            foreach (TAttackComponent attackComponent in _attackComponents)
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

            TAttackComponent selectedAttackComponent = possibleAttacks[randomNumber];
            
            minimumRangesInsideCurrentRange.RemoveAt(randomNumber);
            maximumRangesInsideCurrentRange.RemoveAt(randomNumber);

            if (minimumRangesInsideCurrentRange.Count == 0)
            {
                _context.SetMinimumRangeToAttack(_maximumRangeToCastAnAttack);
                _context.SetMaximumRangeToAttack(_minimumRangeToCastAnAttack);
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

        public virtual void OnAttackAvailableAgain(TAttackComponent attackComponent)
        {
            float attackMinimumRangeToCast = attackComponent.GetMinimumRangeCast();
            float attackMaximumRangeToCast = attackComponent.GetMaximumRangeCast();

            if (_context.GetMinimumRangeToAttack() > attackMinimumRangeToCast)
            {
                _context.SetMinimumRangeToAttack(attackMinimumRangeToCast);
            }

            if (_context.GetMaximumRangeToAttack() > attackMaximumRangeToCast)
            {
                return;
            }
            
            _context.SetMaximumRangeToAttack(attackMaximumRangeToCast);
        }

        protected abstract void UpdateVisibleRivals();
        protected abstract void CalculateBestAction();

        public abstract void OnReceiveDamage(TDamageComponent damageComponent);

        public abstract AIAgentType GetAIAgentType();
        
        public abstract TContext GetContext();

        public abstract void SetLastActionIndex(uint lastActionIndex);
        public abstract void SetHealth(uint health);
        public abstract void SetRivalIndex(uint rivalIndex);

        public abstract void SetRivalRadius(float rivalRadius);
        public abstract void SetDistanceToRival(float rivalDistance);
        
        public abstract void SetIsSeeingARival(bool isSeeingARival);
        public abstract void SetHasATarget(bool hasATarget);
        public abstract void SetIsFighting(bool isFighting);
        public abstract void SetIsAttacking(bool isAttacking);

        public abstract void SetVectorToRival(Vector3 vectorToRival);

        public abstract void SetRivalTransform(Transform rivalTransform);

        public abstract IStatWeight GetStatWeightComponent();

        public uint GetCombatAgentInstance()
        {
            return _combatAgentInstanceID;
        }

        public List<uint> GetVisibleRivals()
        {
            return _visibleRivals;
        }

        public IGroup GetGroupComponent()
        {
            return _groupComponent;
        }

        protected void UpdateVectorToRival()
        {
            TContext context = GetContext();

            if (!context.HasATarget())
            {
                return;
            }

            Vector3 rivalPosition = context.GetRivalTransform().position;
            
            context.SetVectorToRival(rivalPosition - transform.position);
        }

        protected void UpdateMinimumRangeToCast(List<float> minimumRangesInsideCurrentRange)
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
            
            GetContext().SetMinimumRangeToAttack(newMinimumRange);
        }

        protected void UpdateMaximumRangeToCast(List<float> maximumRangesInsideCurrentRange)
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
            
            GetContext().SetMaximumRangeToAttack(newMaximumRange);
        }

        public void DebugMessage(string damage,  string attackerName)
        {
            Debug.Log(name + " gonna receive " + damage + " damage by " + attackerName);
        }
    }
}
