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
    public abstract class AICombatAgentEntity : NavMeshAgentEntity
    {
        private List<AttackComponent> _attackComponents = new List<AttackComponent>();

        protected DamageFeedbackComponent _damageFeedbackComponent;

        protected DefeatComponent _defeatComponent;

        protected AICombatAgentContext _aiCombatAgentContext;

        protected IStatWeight _statWeightComponent;

        protected IGroup _groupComponent;

        [SerializeField] private float _radius;

        protected void SetupCombatComponents(AICombatAgentSpecs aiCombatAgentSpecs)
        {
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

        public abstract AIAgentType GetAIAgentType();

        public ref AICombatAgentContext GetContext()
        {
            return ref _aiCombatAgentContext;
        }

        public List<AttackComponent> GetAttackComponents()
        {
            return _attackComponents;
        }

        public IStatWeight GetStatWeightComponent()
        {
            return _statWeightComponent;
        }

        public IGroup GetGroupComponent()
        {
            return _groupComponent;
        }

        public float GetRadius()
        {
            return _radius;
        }

        public void DebugMessage(string damage,  string attackerName)
        {
            Debug.Log(name + " gonna receive " + damage + " damage by " + attackerName);
        }
    }
}
