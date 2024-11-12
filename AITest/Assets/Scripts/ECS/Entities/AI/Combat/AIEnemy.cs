using System.Collections;
using System.Collections.Generic;
using AI;
using AI.Combat;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using Interfaces.AI.Combat;
using Managers;
using UnityEngine;

namespace ECS.Entities.AI.Combat
{
    public class AIEnemy : AICombatAgentEntity<AIEnemyContext, AttackComponent, AllyDamageComponent>
    {
        [SerializeField] private AIEnemySpecs _aiEnemySpecs;

        [SerializeField] private SphereCollider _originalThreatGroupInfluenceCollider;

        private ThreatComponent _threatComponent;
        
        private void Start()
        {
            Setup();
            SetupCombatComponents(_aiEnemySpecs);
            InstantiateAttackComponents(_aiEnemySpecs.aiAttacks);
            CalculateMinimumAndMaximumRangeToAttacks(_attackComponents);
            
            _threatComponent = new ThreatComponent(_aiEnemySpecs.threatLevel);
            _groupComponent = _threatComponent;
            _context = new AIEnemyContext(_aiEnemySpecs.totalHealth, _threatComponent.GetCurrentGroup(), 
                GetComponent<CapsuleCollider>().radius, _aiEnemySpecs.sightMaximumDistance, _minimumRangeToCastAnAttack, 
                _maximumRangeToCastAnAttack, transform, _aiEnemySpecs.threatLevel, 
                _originalThreatGroupInfluenceCollider.radius, _aiEnemySpecs.maximumStress, _aiEnemySpecs.stunDuration);
            
            CombatManager.Instance.AddAIEnemy(this);
            
            StartUpdate();
        }

        private void InstantiateAttackComponents(List<AIAttack> attacks)
        {
            foreach (AIAttack aiAttack in attacks)
            {
                switch (aiAttack.aiAttackAoEType)
                {
                    case AIAttackAoEType.RECTANGLE_AREA:
                        _attackComponents.Add(new RectangleAttackComponent(aiAttack));
                        break;
                    
                    case AIAttackAoEType.CIRCLE_AREA:
                        _attackComponents.Add(new CircleAttackComponent(aiAttack));
                        break;
                    
                    case AIAttackAoEType.CONE_AREA:
                        _attackComponents.Add(new ConeAttackComponent(aiAttack));
                        break;
                }
            }
        }

        protected override IEnumerator UpdateCoroutine()
        {
            while (true)
            {
                UpdateVisibleRivals();

                UpdateVectorToRival();

                if (_context.IsAttacking())
                {
                    yield return null;
                    continue;
                }
            
                CalculateBestAction();

                yield return null;
            }
        }

        protected override void UpdateVisibleRivals()
        {
            _visibleRivals = CombatManager.Instance.GetVisibleRivals
                <AIAlly, AIAllyContext, AllyAttackComponent, DamageComponent, 
                    AIEnemyContext, AttackComponent, AllyDamageComponent>(this);

            _context.SetIsSeeingARival(_visibleRivals.Count != 0);
        }

        protected override void CalculateBestAction()
        {
            CombatManager.Instance.CalculateBestAction(this);
        }

        public AttackComponent Attack()
        {
            StopNavigation();

            AttackComponent attackComponent = ReturnNextAttack();
            
            Attacking();

            return attackComponent;
        }

        public override void OnReceiveDamage(AllyDamageComponent damageComponent)
        {
            _context.SetHealth(_context.GetHealth() - damageComponent.GetDamage());

            uint combatAgentInstanceID = GetCombatAgentInstance();

            uint health = _context.GetHealth();

            if (health == 0)
            {
                OnDefeated();
                return;
            }
            
            bool isStunned = _context.IsStunned();

            if (!isStunned)
            {
                _context.SetCurrentStress(_context.GetCurrentStress() + damageComponent.GetStressDamage());
                isStunned = _context.IsStunned();
                
                if (isStunned)
                {
                    StartCoroutine(StunDuration());
                }
                else
                {
                    StartCoroutine(DamageFeedback());
                }
                
                CombatManager.Instance.OnEnemyReceiveDamage(combatAgentInstanceID, health, _context.GetCurrentStress(), isStunned);
                return;
            }
            
            CombatManager.Instance.OnEnemyReceiveDamage(combatAgentInstanceID, health, _context.GetCurrentStress(), true);
        }

        protected override void OnDefeated()
        {
            CombatManager.Instance.OnEnemyDefeated(this);
            ECSNavigationManager.Instance.RemoveNavMeshAgentEntity(GetNavMeshAgentComponent());
            Destroy(gameObject);
        }

        private IEnumerator StunDuration()
        {
            float stunDuration = GetContext().GetStunDuration();
            float time = 0;

            GetNavMeshAgentComponent().GetNavMeshAgent().isStopped = true;

            _damageFeedbackComponent.GetMeshRenderer().material.color = new Color(0.71f, 0, 0.63f);
            
            while (time < stunDuration)
            {
                time += Time.deltaTime;
                yield return null;
            }

            GetNavMeshAgentComponent().GetNavMeshAgent().isStopped = false;
            
            GetContext().SetIsStunned(false);
            
            _damageFeedbackComponent.GetMeshRenderer().material.color = Color.red;
            
            RotateToNextPathCorner();
            CombatManager.Instance.OnEnemyStunEnds(GetCombatAgentInstance());
        }

        public override AIAgentType GetAIAgentType()
        {
            return _aiEnemySpecs.aiAgentType;
        }

        public override AIEnemyContext GetContext()
        {
            return _context;
        }

        public void SetCurrentThreatGroup(uint currentThreatGroup)
        {
            _threatComponent.currentGroup = currentThreatGroup;
            _context.SetCurrentGroup(currentThreatGroup);
        }

        public void SetCurrentStress(float currentStress)
        {
            _context.SetCurrentStress(currentStress);
        }

        public override IStatWeight GetStatWeightComponent()
        {
            return _threatComponent;
        }

        public List<AttackComponent> GetAttackComponents()
        {
            return _attackComponents;
        }

        public ThreatComponent GetThreatComponent()
        {
            return _threatComponent;
        }
    }
}
