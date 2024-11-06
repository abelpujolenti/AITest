using System.Collections;
using System.Collections.Generic;
using AI;
using AI.Combat;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using ECS.Entities.AI.Navigation;
using Interfaces.AI.Combat;
using UnityEngine;

namespace ECS.Entities.AI.Combat
{
    public abstract class AICombatAgentEntity<TContext> : NavMeshAgentEntity where TContext : AICombatAgentContext
    {
        private uint _combatAgentInstanceID;

        protected List<uint> _visibleRivals = new List<uint>();
        
        private List<AttackComponent> _attackComponents = new List<AttackComponent>();

        protected DamageFeedbackComponent _damageFeedbackComponent;

        protected DefeatComponent _defeatComponent;

        protected IGroup _groupComponent;

        private Coroutine _updateCoroutine;

        protected void StartUpdate()
        {
            _updateCoroutine = StartCoroutine(UpdateCoroutine());
        }

        protected void StopUpdate()
        {
            StopCoroutine(_updateCoroutine);
        }

        protected abstract IEnumerator UpdateCoroutine();

        protected void SetupCombatComponents(AICombatAgentSpecs aiCombatAgentSpecs)
        {
            _combatAgentInstanceID = (uint)gameObject.GetInstanceID();
            
            _damageFeedbackComponent = new DamageFeedbackComponent(GetComponent<MeshRenderer>(), 
                aiCombatAgentSpecs.flashTime, aiCombatAgentSpecs.flashColor);
            
            foreach (AIAttack aiAttack in aiCombatAgentSpecs.aiAttacks)
            {
                switch (aiAttack.attackAoE.aiAttackAoEType)
                {
                    case AIAttackAoEType.RECTANGLE_AREA:
                        AddRectangleAttack(aiAttack);
                        break;
                    
                    case AIAttackAoEType.CIRCLE_AREA:
                        AddCircleAttack(aiAttack);
                        break;
                    
                    case AIAttackAoEType.CONE_AREA:
                        AddConeAttack(aiAttack);
                        break;
                }
            }
        }

        private void AddRectangleAttack(AIAttack aiAttack)
        {
            _attackComponents.Add(new RectangleAttackComponent(aiAttack, aiAttack.attackAoE));
        }
        
        private void AddCircleAttack(AIAttack aiAttack)
        {
            _attackComponents.Add(new CircleAttackComponent(aiAttack, aiAttack.attackAoE));
        }
        
        private void AddConeAttack(AIAttack aiAttack)
        {
            _attackComponents.Add(new ConeAttackComponent(aiAttack, aiAttack.attackAoE));
        }

        protected abstract void UpdateVisibleRivals();
        protected abstract void CalculateBestAction();
        
        public abstract void Attack(uint attackIndex); 
        public abstract void OnReceiveDamage(DamageComponent damageComponent);

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

        public List<AttackComponent> GetAttackComponents()
        {
            return _attackComponents;
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

        public void DebugMessage(string damage,  string attackerName)
        {
            Debug.Log(name + " gonna receive " + damage + " damage by " + attackerName);
        }
    }
}
