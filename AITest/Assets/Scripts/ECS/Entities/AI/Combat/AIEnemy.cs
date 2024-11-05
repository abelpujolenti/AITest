using AI;
using AI.Combat.Enemy;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using Interfaces.AI.Combat;
using Managers;
using UnityEngine;

namespace ECS.Entities.AI.Combat
{
    public class AIEnemy : AICombatAgentEntity<AIEnemyContext>
    {
        [SerializeField] private AIEnemySpecs _aiEnemySpecs;

        private AIEnemyContext _enemyContext;

        private ThreatComponent _threatComponent;
        
        private void Start()
        {
            Setup();
            SetupCombatComponents(_aiEnemySpecs);
            _threatComponent = new ThreatComponent(_aiEnemySpecs.threatLevel);
            _enemyContext = new AIEnemyContext(_aiEnemySpecs.totalHealth, transform, 
                _aiEnemySpecs.threatLevel, _aiEnemySpecs.maximumStress);
            
            CombatManager.Instance.AddAIEnemy(this, _enemyContext);
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

        public override void Attack(uint attackIndex)
        {
            //TODO
            _enemyContext.SetIsAttacking(true);
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
