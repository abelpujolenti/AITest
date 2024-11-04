using AI;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using Managers;
using UnityEngine;

namespace ECS.Entities.AI.Combat
{
    public class AIAlly : AICombatAgentEntity
    {
        [SerializeField] private AIAllySpecs _aiAllySpecs;
        
        private MoralComponent _moralComponent;
        
        private DieComponent _dieComponent;

        private void Start()
        {
            Setup();
            SetupCombatComponents(_aiAllySpecs);
            _moralComponent = new MoralComponent(_aiAllySpecs.moralWeight);
            _statWeightComponent = _moralComponent;
            _dieComponent = new DieComponent();
            _aiCombatAgentContext = new AIAllyContext(_aiAllySpecs.totalHealth, transform, _aiAllySpecs.aiAttacks[0].maximumRangeCast,
                _aiAllySpecs.aiAttacks[0].totalDamage, _aiAllySpecs.basicStressDamage, _aiAllySpecs.moralWeight);
            
            ECSCombatManager.Instance.AddAIAlly(this, (AIAllyContext)_aiCombatAgentContext);
        }

        public ref MoralComponent GetMoralComponent()
        {
            return ref _moralComponent;
        }
        
        public DieComponent GetDieComponent()
        {
            return _dieComponent;
        }

        public override AIAgentType GetAIAgentType()
        {
            return _aiAllySpecs.aiAgentType;
        }
    }
}