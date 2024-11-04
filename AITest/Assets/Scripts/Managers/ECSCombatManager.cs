using System;
using System.Collections.Generic;
using AI;
using AI.Combat;
using AI.Combat.Ally;
using AI.Combat.Enemy;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using ECS.Components.AI.Navigation;
using ECS.Entities.AI.Combat;
using ECS.Systems.AI.Combat;
using Interfaces.AI.Combat;
using Interfaces.AI.Navigation;
using UnityEngine;

namespace Managers
{
    public class ECSCombatManager : MonoBehaviour
    {
        private static ECSCombatManager _instance;

        public static ECSCombatManager Instance => _instance;

        private Dictionary<AIAgentType, List<AICombatAgentEntity>> _aiCombatAgents =
            new Dictionary<AIAgentType, List<AICombatAgentEntity>>
            {
                { AIAgentType.ALLY, new List<AICombatAgentEntity>() },
                { AIAgentType.ENEMY, new List<AICombatAgentEntity>() }
            };

        private Dictionary<AIAgentType, int> _targetsLayerMask = new Dictionary<AIAgentType, int>
        {
            { AIAgentType.ALLY, (int)(Math.Pow(2, 7) + Math.Pow(2, 9)) },
            { AIAgentType.ENEMY, (int)(Math.Pow(2, 8) + Math.Pow(2, 9)) }
        };

        private Dictionary<AICombatAgentEntity, AIAllyContext> _aiAllyAgentsContexts =
            new Dictionary<AICombatAgentEntity, AIAllyContext>();

        private Dictionary<AICombatAgentEntity, AIEnemyContext> _aiEnemyAgentsContexts =
            new Dictionary<AICombatAgentEntity, AIEnemyContext>();

        private Dictionary<AIAllyAction, Action<AIAlly>> _aiAllyActions = new Dictionary<AIAllyAction, Action<AIAlly>>
        {
            { AIAllyAction.FOLLOW_PLAYER , ally => Debug.Log(ally.name + " Following Player") /*Instance.AllyRequestRival(ally)*/ },
            { AIAllyAction.LOOK_FOR_RIVAL , ally => Instance.AllyRequestRival(ally) },
            { AIAllyAction.GET_CLOSER_TO_RIVAL , ally => Debug.Log(ally.name + " Getting Closer To Rival") /*Instance.AllyRequestRival(ally)*/ },
            { AIAllyAction.ATTACK , ally => Debug.Log(ally.name + " Attacking") /*Instance.AllyRequestRival(ally)*/ },
            { AIAllyAction.FLEE , ally => Debug.Log(ally.name + " Fleeing") /*Instance.AllyRequestRival(ally)*/ },
            { AIAllyAction.DODGE_ATTACK , ally => Debug.Log(ally.name + " Dodging") /*Instance.AllyRequestRival(ally)*/ },
            { AIAllyAction.HELP_ALLY , ally => Debug.Log(ally.name + " Helping Ally") /*Instance.AllyRequestRival(ally)*/ },
        };
        
        private Dictionary<AIEnemyAction, Action<AIEnemy>> _aiEnemyActions = new Dictionary<AIEnemyAction, Action<AIEnemy>>
        {
            { AIEnemyAction.LOOK_FOR_RIVAL , enemy => Instance.EnemyRequestRival(enemy)},
            { AIEnemyAction.GET_CLOSER_TO_RIVAL , enemy => Debug.Log(enemy.name + " Getting Closer To Rival") /*Instance.EnemyRequestRival(enemy)*/},
            { AIEnemyAction.ATTACK , enemy => Debug.Log(enemy.name + " Attacking") /*Instance.EnemyRequestRival(enemy)*/},
            { AIEnemyAction.FLEE , enemy => Debug.Log(enemy.name + " Fleeing") /*Instance.EnemyRequestRival(enemy)*/}
        };

        private Dictionary<AICombatAgentEntity, List<AICombatAgentEntity>> _aiRivalsInLineOfSight =
            new Dictionary<AICombatAgentEntity, List<AICombatAgentEntity>>();

        private Dictionary<AICombatAgentEntity, AICombatAgentEntity> _aiCombatAgentsTargets =
            new Dictionary<AICombatAgentEntity, AICombatAgentEntity>();

        private Dictionary<AttackComponent, AIAttackCollider> _attacksColliders =
            new Dictionary<AttackComponent, AIAttackCollider>();

        private Dictionary<AIEnemy, List<AIEnemy>> _enemiesOverlappingTriggers =
            new Dictionary<AIEnemy, List<AIEnemy>>();

        private Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> _groupThreatsComponents =
                new Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>>();

        private Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>> _groupThreatWeightAndOrigin =
                new Dictionary<uint, KeyValuePair<GroupThreatWeightComponent, VectorComponent>>();

        private ThreatSystem _threatSystem = new ThreatSystem();
        private MoralSystem _moralSystem = new MoralSystem();
        private CombatSystem _combatSystem = new CombatSystem();
        private AttackSystem _attackSystem = new AttackSystem();
        private FleeSystem _fleeSystem = new FleeSystem();
        //private ReceiveDamageSystem _receiveDamageSystem = new ReceiveDamageSystem();
        
        //ERASE!!!
        [SerializeField] private List<GameObject> FLEE_POINTS;
        [SerializeField] private List<Vector3> TERRAIN_POSITIONS;
        private Dictionary<AIAlly, int> FLEE_POINTS_RECORD = new Dictionary<AIAlly, int>(); 
        //

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;

                DontDestroyOnLoad(gameObject);
                
                //ERASE!!!
                TERRAIN_POSITIONS = _fleeSystem.GetTerrainPositions(FLEE_POINTS);
                //

                return;
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            _threatSystem.UpdateThreatGroupsBarycenter(ref _groupThreatsComponents, ref _groupThreatWeightAndOrigin);
            
            UpdateVisibleRivals();
            
            CalculateBestAction();

            /*_combatSystem.UpdateCombatState(_aiCombatAgentsTargets);
            
            _fleeSystem.UpdateFleeMovement(ref FLEE_POINTS_RECORD, TERRAIN_POSITIONS);*/
        }

        private void UpdateVisibleRivals()
        {
            foreach (AIAgentType aiAgentType in _aiCombatAgents.Keys)
            {
                foreach (AICombatAgentEntity combatAgent in _aiCombatAgents[aiAgentType])
                {
                    List<AICombatAgentEntity> visibleRivals = CheckVisibleRivalsByGivenCombatAgent(combatAgent);

                    _aiRivalsInLineOfSight[combatAgent] = visibleRivals;

                    combatAgent.GetContext().isSeeingARival = visibleRivals.Count != 0;
                }
            }
        }

        private List<AICombatAgentEntity> CheckVisibleRivalsByGivenCombatAgent(AICombatAgentEntity aiCombatAgent)
        {
            AIAgentType ownAgentType = aiCombatAgent.GetAIAgentType();

            List<AICombatAgentEntity> visibleRivals = new List<AICombatAgentEntity>();

            foreach (AIAgentType aiAgentType in _aiCombatAgents.Keys)
            {
                if (aiAgentType == ownAgentType)
                {
                    continue;
                }

                foreach (AICombatAgentEntity targetAICombatAgent in _aiCombatAgents[aiAgentType])
                {
                    if (targetAICombatAgent.GetContext().health == 0)
                    {
                        continue;
                    }
                    
                    if (Physics.Raycast(aiCombatAgent.transform.position, targetAICombatAgent.transform.position,
                            Mathf.Infinity, _targetsLayerMask[ownAgentType]))
                    {
                        continue;
                    }

                    visibleRivals.Add(targetAICombatAgent);
                }
            }

            return visibleRivals;
        }

        private void CalculateBestAction()
        {
            foreach (AICombatAgentEntity ally in _aiCombatAgents[AIAgentType.ALLY])
            {
                AIAllyContext allyContext = _aiAllyAgentsContexts[ally];
                
                AIAllyAction allyAction = AIAllyUtilityFunction.GetBestAction(allyContext);

                if (CheckIfIsAlreadyPerformingThatAction((uint)allyAction, allyContext.lastActionIndex, ally, 
                        ref _aiAllyAgentsContexts))
                {
                    continue;
                }

                _aiAllyActions[allyAction]((AIAlly)ally);
            }

            foreach (AICombatAgentEntity enemy in _aiCombatAgents[AIAgentType.ENEMY])
            {
                AIEnemyContext enemyContext = _aiEnemyAgentsContexts[enemy];
                
                AIEnemyAction enemyAction = AIEnemyUtilityFunction.GetBestAction(enemyContext);

                if (CheckIfIsAlreadyPerformingThatAction((uint)enemyAction, enemyContext.lastActionIndex, enemy, 
                        ref _aiEnemyAgentsContexts))
                {
                    continue;
                }

                _aiEnemyActions[enemyAction]((AIEnemy)enemy);
            }
        }

        private bool CheckIfIsAlreadyPerformingThatAction<TCombatAgentContext>(uint currentAction, uint lastAction, 
            AICombatAgentEntity combatAgent, ref Dictionary<AICombatAgentEntity, TCombatAgentContext> agentContexts)
            where TCombatAgentContext : AICombatAgentContext
        {
            if (lastAction == currentAction)
            {
                return true;
            }

            agentContexts[combatAgent].lastActionIndex = currentAction;

            return false;
        }

        #region Add Combat Agent

        public void AddAIAlly(AIAlly aiAlly, AIAllyContext aiAllyContext)
        {
            AddAlly(aiAlly);
            AddAllyContext(aiAlly, aiAllyContext);
            AddAttack(aiAlly.GetAttackComponents(), GameManager.Instance.GetEnemyLayer());
            ECSNavigationManager.Instance.AddNavMeshAgentEntity(aiAlly.GetNavMeshAgentComponent());
        }

        private void AddAlly(AIAlly aiAlly)
        {
            //TODO moral system

            AddCombatAgent(aiAlly);
        }

        private void AddAllyContext(AIAlly aiAlly, AIAllyContext aiAllyContext)
        {
            _aiAllyAgentsContexts.Add(aiAlly, aiAllyContext);
        }

        private void AddCombatAgent(AICombatAgentEntity aiCombatAgent)
        {
            AIAgentType aiAgentType = aiCombatAgent.GetAIAgentType();

            _aiCombatAgents[aiAgentType].Add(aiCombatAgent);
            _aiRivalsInLineOfSight.Add(aiCombatAgent, new List<AICombatAgentEntity>());
            _aiCombatAgentsTargets.Add(aiCombatAgent, null);
        }

        public void AddAIEnemy(AIEnemy aiEnemy, AIEnemyContext aiEnemyContext)
        {
            AddEnemy(aiEnemy);
            AddEnemyContext(aiEnemy, aiEnemyContext);
            AddAttack(aiEnemy.GetAttackComponents(), GameManager.Instance.GetAllyLayer());
            ECSNavigationManager.Instance.AddNavMeshAgentEntity(aiEnemy.GetNavMeshAgentComponent());
        }

        private void AddEnemy(AIEnemy aiEnemy)
        {
            _enemiesOverlappingTriggers.Add(aiEnemy, new List<AIEnemy>());

            _threatSystem.AddThreat(ref _groupThreatsComponents, ref _groupThreatWeightAndOrigin,
                aiEnemy.GetThreatComponent(), aiEnemy.GetNavMeshAgentComponent().GetTransformComponent());

            AddCombatAgent(aiEnemy);
        }

        private void AddEnemyContext(AIEnemy aiEnemy, AIEnemyContext aiEnemyContext)
        {
            _aiEnemyAgentsContexts.Add(aiEnemy, aiEnemyContext);
        }

        private void AddAttack(List<AttackComponent> attackComponents, int layerTarget)
        {
            foreach (AttackComponent attackComponent in attackComponents)
            {
                GameObject colliderObject = new GameObject();

                switch (attackComponent.GetAIAttackAoEType())
                {
                    case AIAttackAoEType.RECTANGLE_AREA:
                        AIRectangleAttackCollider rectangleAttackCollider =
                            colliderObject.AddComponent<AIRectangleAttackCollider>();
                        
                        rectangleAttackCollider.SetRectangleAttackComponent((RectangleAttackComponent)attackComponent);
                        rectangleAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _attacksColliders.Add(attackComponent, rectangleAttackCollider);
                        break;

                    case AIAttackAoEType.CIRCLE_AREA:
                        AICircleAttackCollider circleAttackCollider =
                            colliderObject.AddComponent<AICircleAttackCollider>();
                        
                        circleAttackCollider.SetCircleAttackComponent((CircleAttackComponent)attackComponent);
                        circleAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _attacksColliders.Add(attackComponent, circleAttackCollider);
                        break;

                    case AIAttackAoEType.CONE_AREA:
                        AIConeAttackCollider coneAttackCollider = colliderObject.AddComponent<AIConeAttackCollider>();
                        coneAttackCollider.SetConeAttackComponent((ConeAttackComponent)attackComponent);
                        coneAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _attacksColliders.Add(attackComponent, coneAttackCollider);
                        break;
                }

                colliderObject.SetActive(false);
            }
        }

        #endregion

        #region Combat Agents Events

        public void OnAllyJoinAlly(ref MoralComponent moralComponent)
        {
            moralComponent.AddMinMoralWeight();
        }

        public void OnAllySeparateFromAlly(ref MoralComponent moralComponent)
        {
            moralComponent.SubtractMinMoralWeight();
        }

        public void OnEnemyJoinEnemy(AIEnemy aiEnemy, AIEnemy otherEnemy)
        {
            _enemiesOverlappingTriggers[aiEnemy].Add(otherEnemy);

            uint aiEnemyThreatGroup = aiEnemy.GetThreatComponent().currentThreatGroup;
            uint otherEnemyThreatGroup = otherEnemy.GetThreatComponent().currentThreatGroup;

            if (aiEnemyThreatGroup < otherEnemyThreatGroup)
            {
                MergeThreatGroups(otherEnemyThreatGroup, aiEnemyThreatGroup);
                return;
            }

            MergeThreatGroups(aiEnemyThreatGroup, otherEnemyThreatGroup);
        }

        private void MergeThreatGroups(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            _threatSystem.MoveWholeThreatGroupToAnotherThreatGroup(ref _groupThreatsComponents,
                ref _groupThreatWeightAndOrigin,
                threatGroupFromWhichTheyCome, threatGroupToMove);
            
            UpdateThreatInAllyContext(threatGroupFromWhichTheyCome, threatGroupToMove);
        }

        public void OnEnemySeparateFromEnemy(AIEnemy aiEnemy, AIEnemy otherEnemy)
        {
            _enemiesOverlappingTriggers[aiEnemy].Remove(otherEnemy);

            List<AIEnemy> allContacts = GetAllContacts(aiEnemy);

            uint lowestThreatGroup = FindLowestThreatGroupIndex(allContacts);
            uint otherThreatGroup = otherEnemy.GetThreatComponent().currentThreatGroup;

            if (lowestThreatGroup == otherThreatGroup)
            {
                return;
            }

            List<ThreatComponent> threatComponents = new List<ThreatComponent>();

            foreach (AIEnemy aiEnemyInContact in allContacts)
            {
                threatComponents.Add(aiEnemyInContact.GetThreatComponent());
            }

            _threatSystem.MoveGivenThreatsToAnotherThreatGroup(ref _groupThreatsComponents,
                ref _groupThreatWeightAndOrigin, threatComponents, otherThreatGroup,
                lowestThreatGroup);

            UpdateThreatInAllyContext(otherThreatGroup, lowestThreatGroup);
        }

        private void UpdateThreatInAllyContext(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            foreach (KeyValuePair<AICombatAgentEntity, AIAllyContext> allyContext in _aiAllyAgentsContexts)
            {
                AIAllyContext aiAllyContext = allyContext.Value;
                
                if (aiAllyContext.threatGroupOfTarget == threatGroupFromWhichTheyCome)
                {
                    aiAllyContext.threatGroupOfTarget = threatGroupToMove;
                }

                aiAllyContext.threatWeightOfTarget = _groupThreatWeightAndOrigin[threatGroupToMove].Key.groupThreatWeight;
            }
        }

        private List<AIEnemy> GetAllContacts(AIEnemy aiEnemy)
        {
            List<AIEnemy> contacts = new List<AIEnemy>();

            Stack<AIEnemy> aiEnemiesStack = new Stack<AIEnemy>();
            aiEnemiesStack.Push(aiEnemy);

            while (aiEnemiesStack.Count > 0)
            {
                AIEnemy currentEnemy = aiEnemiesStack.Pop();

                contacts.Add(currentEnemy);

                foreach (AIEnemy aiEnemyToCheck in _enemiesOverlappingTriggers[currentEnemy])
                {
                    if (!contacts.Contains(aiEnemyToCheck))
                    {
                        aiEnemiesStack.Push(aiEnemyToCheck);
                    }
                }
            }

            return contacts;
        }

        private uint FindLowestThreatGroupIndex(List<AIEnemy> aiEnemies)
        {
            uint lowestThreatGroupIndex = aiEnemies[0].GetThreatComponent().GetOriginalThreatGroup();

            foreach (AIEnemy aiEnemy in aiEnemies)
            {
                uint currentOriginalThreatGroupIndex = aiEnemy.GetThreatComponent().GetOriginalThreatGroup();

                if (lowestThreatGroupIndex < currentOriginalThreatGroupIndex)
                {
                    continue;
                }

                lowestThreatGroupIndex = currentOriginalThreatGroupIndex;
            }

            return lowestThreatGroupIndex;
        }

        private void AllyRequestRival(AIAlly ally)
        {
            Debug.Log(ally.name + " Requesting Rival");
                
            _aiRivalsInLineOfSight[ally] =
                FilterPerThreatWeight(ally.GetStatWeightComponent(), _aiRivalsInLineOfSight[ally]);

            _aiRivalsInLineOfSight[ally] = FilterPerThreatGroup(ally, _aiRivalsInLineOfSight[ally]);
            
            if (_aiRivalsInLineOfSight[ally].Count == 0)
            {
                return;
            }
            
            AICombatAgentEntity target = RequestRival(ally, _aiRivalsInLineOfSight[ally]); 

            _aiAllyAgentsContexts[ally].enemyHealth = _aiEnemyAgentsContexts[_aiCombatAgentsTargets[ally]].health;

            ECSNavigationManager.Instance.UpdateNavMeshAgentTransformDestination(ally.GetNavMeshAgentComponent(), 
                AssignTarget(ally, target));
        }

        private List<AICombatAgentEntity> FilterPerThreatWeight(IStatWeight moralWeightComponent,
            List<AICombatAgentEntity> possibleTargetsAICombatAgentEntities)
        {
            for (int i = possibleTargetsAICombatAgentEntities.Count - 1; i >= 0 ; i--)
            {
                AICombatAgentEntity enemy = possibleTargetsAICombatAgentEntities[i];
                
                if (_moralSystem.EvaluateConfrontation(moralWeightComponent,
                        enemy.GetStatWeightComponent()))
                {
                    continue;
                }

                possibleTargetsAICombatAgentEntities.Remove(enemy);
            }

            return possibleTargetsAICombatAgentEntities;
        }

        private List<AICombatAgentEntity> FilterPerThreatGroup(AICombatAgentEntity combatAgent,
            List<AICombatAgentEntity> possibleTargetsAICombatAgentEntities)
        {
            uint[] whichThreatGroupsAreAlliesFighting = new uint[_aiCombatAgents[AIAgentType.ALLY].Count];

            int counter = 0;

            foreach (AICombatAgentEntity ally in _aiCombatAgents[AIAgentType.ALLY])
            {
                if (ally == combatAgent)
                {
                    continue;
                }

                AIAllyContext allyContext = Parse<AIAllyContext, AICombatAgentContext>(ally.GetContext());

                if (allyContext == null)
                {
                    continue;
                }

                whichThreatGroupsAreAlliesFighting[counter] = allyContext.threatGroupOfTarget;
                counter++;
            }

            List<AICombatAgentEntity> enemiesFromOccupiedThreatGroup = new List<AICombatAgentEntity>();

            for (int i = possibleTargetsAICombatAgentEntities.Count - 1; i >= 0; i--)
            {
                AIEnemy enemy = Parse<AIEnemy, AICombatAgentEntity>(possibleTargetsAICombatAgentEntities[i]);
                
                uint threatGroup = enemy.GetThreatComponent().currentThreatGroup;

                for (int j = 0; j < whichThreatGroupsAreAlliesFighting.Length; j++)
                {
                    if (threatGroup != whichThreatGroupsAreAlliesFighting[i])
                    {
                        continue;
                    }
                    
                    enemiesFromOccupiedThreatGroup.Add(enemy);
                    possibleTargetsAICombatAgentEntities.Remove(enemy);
                }
            }

            if (possibleTargetsAICombatAgentEntities.Count == 0)
            {
                return enemiesFromOccupiedThreatGroup;
            }

            return possibleTargetsAICombatAgentEntities;
        }

        private void EnemyRequestRival(AIEnemy enemy)
        {
            Debug.Log(enemy.name + " Requesting Rival");
            
            if (_aiRivalsInLineOfSight[enemy].Count == 0)
            {
                return;
            }
            
            AICombatAgentEntity target = RequestRival(enemy, _aiRivalsInLineOfSight[enemy]);

            ECSNavigationManager.Instance.UpdateNavMeshAgentTransformDestination(enemy.GetNavMeshAgentComponent(), 
                AssignTarget(enemy, target));
        }

        private AICombatAgentEntity RequestRival(AICombatAgentEntity aiCombatAgent, List<AICombatAgentEntity> rivals)
        {
            if (rivals.Count == 1)
            {
                return rivals[0];
            }
            
            return GetClosestRival(aiCombatAgent.GetNavMeshAgentComponent().GetTransformComponent(), rivals);
        }

        private TransformComponent AssignTarget(AICombatAgentEntity attacker, AICombatAgentEntity target)
        {
            _aiCombatAgentsTargets[attacker] = target;

            attacker.GetContext().hasATarget = true;
            attacker.GetContext().rivalTransform = target.transform;

            return target.GetNavMeshAgentComponent().GetTransformComponent();
        }

        private AICombatAgentEntity GetClosestRival(IPosition positionComponent,
            List<AICombatAgentEntity> possibleTargetsAICombatAgentEntities)
        {
            AICombatAgentEntity target = null;
            AICombatAgentEntity currentTarget;

            float targetDistance = 300000000;
            float currentTargetDistance;

            for (int i = 0; i < possibleTargetsAICombatAgentEntities.Count; i++)
            {
                currentTarget = possibleTargetsAICombatAgentEntities[i];

                currentTargetDistance = (currentTarget.transform.position - positionComponent.GetPosition()).magnitude;

                if (currentTargetDistance >= targetDistance)
                {
                    continue;
                }

                target = currentTarget;
                targetDistance = currentTargetDistance;
            }

            return target;
        }

        public void OnAllyReceiveDamage(AIAlly aiAllyTarget, DamageComponent damageComponent)
        {
            _aiAllyAgentsContexts[aiAllyTarget].health -= damageComponent.GetDamage();

            if (!CheckDefeat(_aiAllyAgentsContexts[aiAllyTarget]))
            {
                return;
            }
            
            OnAllyDefeated(aiAllyTarget);
        }
        
        private void OnAllyDefeated(AIAlly aiAlly)
        {
            //TODO
            
            OnAgentDefeated(aiAlly);
        }

        public void OnEnemyReceiveDamage(AIEnemy aiEnemyTarget, DamageComponent damageComponent)
        {
            _aiEnemyAgentsContexts[aiEnemyTarget].health -= damageComponent.GetDamage();

            if (CheckDefeat(_aiEnemyAgentsContexts[aiEnemyTarget]))
            {
                OnEnemyDefeated(aiEnemyTarget);
                return;
            }
            
            foreach (var attackerAndTarget in _aiCombatAgentsTargets)
            {
                AICombatAgentEntity attacker = attackerAndTarget.Key;

                if (attacker.GetAIAgentType() != AIAgentType.ALLY)
                {
                    continue;
                }
                
                if (attackerAndTarget.Value != aiEnemyTarget)
                {
                    continue;
                }

                _aiAllyAgentsContexts[attacker].enemyHealth = _aiEnemyAgentsContexts[aiEnemyTarget].health;
            }
        }

        public void OnEnemyDefeated(AIEnemy aiEnemy)
        {
            _aiCombatAgents[AIAgentType.ENEMY].Remove(aiEnemy);

            ThreatComponent threatComponent = aiEnemy.GetThreatComponent();

            uint currentThreatGroup = threatComponent.currentThreatGroup;

            _groupThreatWeightAndOrigin[currentThreatGroup].Key.groupThreatWeight -= threatComponent.GetWeight();

            TransformComponent transformComponent = aiEnemy.GetNavMeshAgentComponent().GetTransformComponent();

            _groupThreatsComponents[currentThreatGroup].Key.Remove(threatComponent);
            _groupThreatsComponents[currentThreatGroup].Value.Remove(transformComponent);

            List<AIEnemy> enemiesOverlapping = _enemiesOverlappingTriggers[aiEnemy];

            for (int i = enemiesOverlapping.Count - 1; i >= 0; i--)
            {
                OnEnemySeparateFromEnemy(enemiesOverlapping[i], aiEnemy);
            }
            
            OnAgentDefeated(aiEnemy);
        }

        private void OnAgentDefeated(AICombatAgentEntity aiCombatAgentDefeated)
        {
            foreach (var attackerAndTarget in _aiCombatAgentsTargets)
            {
                if (attackerAndTarget.Value != aiCombatAgentDefeated)
                {
                    continue;
                }
                
                //RequestRival(attackerAndTarget.Key);
            }
        }

        private bool CheckDefeat(AICombatAgentContext aiCombatAgentContext)
        {
            return aiCombatAgentContext.health == 0;
        }

        #endregion

        #region Flee Events

        public void RequestSafeSpot(AICombatAgentEntity aiCombatAgentEntity)
        {
            
        }

        #endregion

        #region Attack Events

        public void StartCastingAnAttack(Transform attackerTransform, AttackComponent attackComponent)
        {
            AIAttackCollider attackCollider = _attacksColliders[attackComponent];
            attackCollider.SetParent(attackerTransform);
            attackCollider.gameObject.SetActive(true);
            StartCoroutine(_attackSystem.StartCastTime(attackComponent, attackCollider));
        }

        public void PutAttackOnCooldown(AttackComponent attackComponent)
        {
            StartCoroutine(_attackSystem.StartCooldown(attackComponent));
        }

        #endregion

        private TParseTo Parse<TParseTo, TParseFrom>(TParseFrom parseFrom)
        where TParseTo : TParseFrom
        {
            TParseTo parseTo;

            try
            {
                parseTo = (TParseTo)parseFrom;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return default;
            }

            return parseTo;
        }
    }
}