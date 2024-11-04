using AI;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using Managers;
using UnityEngine;

namespace ECS.Entities.AI.Combat
{
    public class AIEnemy : AICombatAgentEntity
    {
        [SerializeField] private AIEnemySpecs _aiEnemySpecs;

        private ThreatComponent _threatComponent;
        
        private void Start()
        {
            Setup();
            SetupCombatComponents(_aiEnemySpecs);
            _threatComponent = new ThreatComponent(_aiEnemySpecs.threatLevel);
            _statWeightComponent = _threatComponent;
            _aiCombatAgentContext = new AIEnemyContext(_aiEnemySpecs.totalHealth, transform, _aiEnemySpecs.threatLevel);
            
            ECSCombatManager.Instance.AddAIEnemy(this, (AIEnemyContext)_aiCombatAgentContext);
        }

        public ThreatComponent GetThreatComponent()
        {
            return _threatComponent;
        }

        //CHANGE IT
        private void OnDestroy()
        {
            ECSCombatManager.Instance.OnEnemyDefeated(this);
        }

        public override AIAgentType GetAIAgentType()
        {
            return _aiEnemySpecs.aiAgentType;
        }
    }
}
