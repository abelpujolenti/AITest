using System;
using System.Collections;
using System.Collections.Generic;
using AI;
using AI.Combat;
using AI.Combat.Ally;
using AI.Combat.Enemy;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using ECS.Components.AI.Navigation;
using ECS.Entities.AI.Combat;
using Interfaces.AI.Combat;
using Interfaces.AI.Navigation;
using Interfaces.AI.UBS.BaseInterfaces.Get;
using Unity.AI.Navigation;
using UnityEngine;
using Utilities;

namespace Managers
{
    public class CombatManager : MonoBehaviour
    {
        private static CombatManager _instance;

        public static CombatManager Instance => _instance;

        [SerializeField] private NavMeshSurface _allyNavMeshSurface;
        [SerializeField] private GameObject _enemyRectangleAttackColliderPrefab;
        [SerializeField] private GameObject _enemyCircleAttackColliderPrefab;

        private Dictionary<AIAgentType, Delegate> _returnTheSameAgentsType = new Dictionary<AIAgentType, Delegate>
        {
            { AIAgentType.ALLY, new Func<List<AIAlly>>(() => 
                _instance.ReturnAllDictionaryValuesInAList<AIAlly ,AIAllyContext, AllyAttackComponent, DamageComponent>(
                    _instance._aiAllies)) },
            
            { AIAgentType.ENEMY, new Func<List<AIEnemy>>(() => 
                _instance.ReturnAllDictionaryValuesInAList<AIEnemy, AIEnemyContext, AttackComponent, AllyDamageComponent>(
                    _instance._aiEnemies)) }
        };
        
        private Dictionary<uint, AIAlly> _aiAllies = new Dictionary<uint, AIAlly>();
        private Dictionary<uint, AIEnemy> _aiEnemies = new Dictionary<uint, AIEnemy>();

        private Dictionary<AIAgentType, int> _targetsLayerMask = new Dictionary<AIAgentType, int>
        {
            { AIAgentType.ALLY, (int)(Math.Pow(2, 7) + Math.Pow(2, 9)) },
            { AIAgentType.ENEMY, (int)(Math.Pow(2, 8) + Math.Pow(2, 9)) }
        };

        private Dictionary<AIAllyAction, Action<AIAlly>> _aiAllyActions = new Dictionary<AIAllyAction, Action<AIAlly>>
        {
            { AIAllyAction.FOLLOW_PLAYER , ally => Instance.AllyFollowPlayer(ally)},
            { AIAllyAction.CHOOSE_NEW_RIVAL , ally => Instance.AllyRequestRival(ally) },
            { AIAllyAction.GET_CLOSER_TO_RIVAL , ally => Instance.AllyGetCloserToEnemy(ally)},
            { AIAllyAction.ROTATE , ally => ally.RotateToNextPathCorner()},
            { AIAllyAction.ATTACK , ally => Instance.AllyAttack(ally)},
            { AIAllyAction.FLEE , ally => Instance.AllyFlee(ally)},
            { AIAllyAction.DODGE_ATTACK , ally => Instance.AllyDodge(ally)},
            { AIAllyAction.HELP_ALLY , ally => Instance.AllyHelpAnotherAlly(ally)}
        };
        
        private Dictionary<AIEnemyAction, Action<AIEnemy>> _aiEnemyActions = new Dictionary<AIEnemyAction, Action<AIEnemy>>
        {
            { AIEnemyAction.PATROL , enemy => Instance.EnemyPatrol(enemy)},
            { AIEnemyAction.CHOOSE_NEW_RIVAL , enemy => Instance.EnemyRequestRival(enemy)},
            { AIEnemyAction.GET_CLOSER_TO_RIVAL , enemy => Instance.EnemyGetCloserToAlly(enemy)},
            { AIEnemyAction.ROTATE , enemy => enemy.RotateToNextPathCorner()},
            { AIEnemyAction.ATTACK , enemy => Instance.EnemyAttack(enemy)},
            { AIEnemyAction.FLEE , enemy => Instance.EnemyFlee(enemy)}
        };

        private Dictionary<AttackComponent, AIEnemyAttackCollider> _enemiesAttacksColliders =
            new Dictionary<AttackComponent, AIEnemyAttackCollider>();

        private Dictionary<AIEnemy, List<uint>> _enemiesOfTheSameThreatGroupOverlappingTriggers =
            new Dictionary<AIEnemy, List<uint>>();

        private Dictionary<uint, List<uint>> _enemiesIndexesInsideThreatGroup = new Dictionary<uint, List<uint>>();

        private Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> _groupThreatsComponents =
                new Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>>();

        private Dictionary<uint, KeyValuePair<ThreatGroupComponent, VectorComponent>> _threatGroups =
                new Dictionary<uint, KeyValuePair<ThreatGroupComponent, VectorComponent>>();

        private List<AIEnemyAttackCollider> _enemyAttackCollidersSubscribedToRebakeAllyNavMesh =
            new List<AIEnemyAttackCollider>();

        private Coroutine _rebakeCoroutine;

        private AIAllyUtilityFunction _allyUtilityFunction = new AIAllyUtilityFunction();
        private AIEnemyUtilityFunction _enemyUtilityFunction = new AIEnemyUtilityFunction();
        
        //ERASE!!!
        [SerializeField] private bool _showActionsDebugLogs;
        [SerializeField] private List<GameObject> FLEE_POINTS;
        private List<Vector3> TERRAIN_POSITIONS;
        private Dictionary<AIAlly, int> FLEE_POINTS_RECORD = new Dictionary<AIAlly, int>(); 
        //

        private void ShowActionDebugLogs(string message)
        {
            if (!_showActionsDebugLogs)
            {
                return;
            }
            
            Debug.Log(message);
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;

                DontDestroyOnLoad(gameObject);
                
                //ERASE!!!
                TERRAIN_POSITIONS = GetTerrainPositions(FLEE_POINTS);
                StartCoroutine(UpdateFleeMovement());
                //

                return;
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            UpdateThreatGroupsBarycenter();

            UpdateThreatGroupsRadius();
        }

        #region UBS
        
        public List<uint> GetVisibleRivals<TAgent, TRivalContext, TRivalAttackComponent, TRivalDamageComponent, 
            TOwnContext, TOwnAttackComponent, TOwnDamageComponent>(
            AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent> aiCombatAgent)
        
            where TAgent : AICombatAgentEntity<TRivalContext, TRivalAttackComponent, TRivalDamageComponent>
            where TRivalContext : AICombatAgentContext
            where TRivalAttackComponent : AttackComponent
            where TRivalDamageComponent : DamageComponent
            where TOwnContext : AICombatAgentContext
            where TOwnAttackComponent : AttackComponent
            where TOwnDamageComponent : DamageComponent
        {
            AIAgentType ownAgentType = aiCombatAgent.GetAIAgentType();

            List<uint> visibleRivals = new List<uint>();

            List<AICombatAgentEntity<TRivalContext, TRivalAttackComponent, TRivalDamageComponent>> rivals = 
                ReturnAllRivals<TAgent, TRivalContext, TRivalAttackComponent, TRivalDamageComponent>(ownAgentType);

            float sightMaximumDistance = aiCombatAgent.GetContext().GetSightMaximumDistance();

            foreach (AICombatAgentEntity<TRivalContext, TRivalAttackComponent, TRivalDamageComponent> rival in rivals)
            {
                if (Physics.Raycast(aiCombatAgent.transform.position, rival.transform.position,
                        sightMaximumDistance, _targetsLayerMask[ownAgentType]))
                {
                    continue;
                }

                visibleRivals.Add(rival.GetCombatAgentInstance());
            }

            return visibleRivals;
        }

        public List<float> GetDistancesToGivenThreatGroups(Vector3 position, List<uint> threatGroups)
        {
            List<float> distancesToThreatGroups = new List<float>();

            foreach (uint threatGroupIndex in threatGroups)
            {
                KeyValuePair<ThreatGroupComponent, VectorComponent> threatGroup = _threatGroups[threatGroupIndex];
                
                distancesToThreatGroups.Add(
                    (threatGroup.Value.GetPosition() - position).magnitude - threatGroup.Key.groupRadius);
            }

            return distancesToThreatGroups;
        }

        public void CalculateBestAction(AIAlly ally)
        {
            AIAllyAction allyAction = CalculateBestAction<AIAllyAction, AIAllyContext>(ally.GetContext(), _allyUtilityFunction);
            
            CheckIfCanPerformGivenAction<AIAlly, AIAllyContext, AllyAttackComponent, DamageComponent, AIAllyAction>(
                ally, allyAction, AllyPerformAction);
        }

        public void CalculateBestAction(AIEnemy enemy)
        {
            AIEnemyAction enemyAction = CalculateBestAction<AIEnemyAction, AIEnemyContext>(enemy.GetContext(), _enemyUtilityFunction);
            
            CheckIfCanPerformGivenAction<AIEnemy, AIEnemyContext, AttackComponent, AllyDamageComponent, AIEnemyAction>(
                enemy, enemyAction, EnemyPerformAction);
        }

        private static TAction CalculateBestAction<TAction, TContext>(TContext context, 
            IGetBestAction<TAction, TContext> utilityCalculator)
        {
            return utilityCalculator.GetBestAction(context);
        }

        private static void CheckIfCanPerformGivenAction<TAgent, TContext, TAttackComponent, TDamageComponent, TAction>(
            TAgent agent, TAction agentAction, Action<TAgent, TAction> action)
        
            where TAgent : AICombatAgentEntity<TContext, TAttackComponent, TDamageComponent> 
            where TContext : AICombatAgentContext
            where TAttackComponent : AttackComponent
            where TDamageComponent : DamageComponent
            where TAction : Enum
        {
            TContext context = agent.GetContext();
            
            uint agentActionUInt = Convert.ToUInt16(agentAction);
            uint lastAction = context.GetLastActionIndex();

            List<uint> repeatableActions = context.GetRepeatableActions();

            if (agentActionUInt == lastAction && !repeatableActions.Contains(lastAction))
            {
                return;
            }
            
            agent.SetLastActionIndex(agentActionUInt);

            action(agent, agentAction);
        }

        #region Ally

        public List<uint> FilterThreatGroupsThatThreatMe(uint combatAgentInstance, IStatWeight moralWeightComponent,
            uint threatGroupOfCurrentTarget, List<uint> visibleRivalsIDs)
        {
            List<uint> threatGroupsThatThreatMe = new List<uint>();
            List<uint> threatGroupsChecked = new List<uint>();

            foreach (uint enemyID in visibleRivalsIDs)
            {
                uint currentThreatGroupID = _aiEnemies[enemyID].GetContext().GetCurrentThreatGroup();

                if (threatGroupsChecked.Contains(currentThreatGroupID))
                {
                    continue;
                }
                
                threatGroupsChecked.Add(currentThreatGroupID);

                ThreatGroupComponent threatGroupComponent = _threatGroups[currentThreatGroupID].Key;

                if (threatGroupComponent.groupTarget != combatAgentInstance)
                {
                    continue;
                }

                float totalThreatWeight = threatGroupComponent.threatGroupWeight;

                if (_threatGroups.ContainsKey(threatGroupOfCurrentTarget))
                {
                    totalThreatWeight += _threatGroups[threatGroupOfCurrentTarget].Key.threatGroupWeight * 
                                         Convert.ToUInt16(currentThreatGroupID != threatGroupOfCurrentTarget);    
                }

                if (totalThreatWeight < moralWeightComponent.GetWeight())
                {
                    continue;
                }
                
                threatGroupsThatThreatMe.Add(currentThreatGroupID);
            }

            return threatGroupsThatThreatMe;
        }

        public uint[] FilterPerThreatGroupAlliesFighting(
            AICombatAgentEntity<AIAllyContext, AllyAttackComponent, DamageComponent> combatAgent)
        {
            uint[] whichThreatGroupsAreAlliesFighting = new uint[_aiAllies.Count - 1];

            int counter = 0;

            foreach (AIAlly ally in _aiAllies.Values)
            {
                if (ally == combatAgent)
                {
                    continue;
                }

                whichThreatGroupsAreAlliesFighting[counter] = ally.GetContext().GetThreatGroupOfTarget();
                counter++;
            }

            return whichThreatGroupsAreAlliesFighting;
        }

        private void AllyPerformAction(AIAlly ally, AIAllyAction allyAction)
        {
            _aiAllyActions[allyAction](ally);
        }

        private void AllyFollowPlayer(AIAlly ally)
        {
            //TODO FOLLOW PLAYER
            ShowActionDebugLogs(ally.name + " Following Player");
            //Debug.Log(ally.name + " Following Player");
        }

        private void AllyRequestRival(AIAlly ally)
        {
            List<uint> visibleRivals = ally.GetVisibleRivals();
            
            ShowActionDebugLogs(ally.name + " Requesting Rival");
            //Debug.Log(ally.name + " Requesting Rival");
            
            if (visibleRivals.Count == 0)
            {
                return;
            }

            List<uint> threatGroupsToAvoid =
                UnifyArraysInAList(ally.GetThreatGroupsThatThreatMe().ToArray(), ally.GetThreatGroupsThatFightAllies());

            List<uint> possibleRivals = GetPossibleRivals(visibleRivals, threatGroupsToAvoid,
                ally.GetStatWeightComponent().GetWeight());

            if (possibleRivals.Count == 0)
            {
                return;
            }

            uint targetId;

            NavMeshAgentComponent navMeshAgentComponent = ally.GetNavMeshAgentComponent();

            if (visibleRivals.Count == 1)
            {
                targetId = visibleRivals[0];
            }
            else
            {
                targetId = GetClosestRival<AIEnemy, AIEnemyContext, AttackComponent, AllyDamageComponent>(
                    navMeshAgentComponent.GetTransformComponent(), _aiEnemies, visibleRivals);
            }

            AIEnemy targetEnemy = _aiEnemies[targetId];

            AIEnemyContext targetEnemyContext = targetEnemy.GetContext(); 
            
            ally.SetRivalIndex(targetEnemy.GetCombatAgentInstance());
            ally.SetRivalRadius(targetEnemyContext.GetRadius());
            ally.SetHasATarget(true);
            ally.SetEnemyHealth(targetEnemyContext.GetHealth());
            ally.SetEnemyMaximumStress(targetEnemyContext.GetMaximumStress());
            ally.SetEnemyCurrentStress(targetEnemyContext.GetCurrentStress());
            ally.SetThreatGroupOfTarget(targetEnemyContext.GetCurrentThreatGroup());
            ally.SetThreatWeightOfTarget(targetEnemyContext.GetCurrentThreatGroupWeight());
            ally.SetRivalTransform(targetEnemyContext.GetAgentTransform());
        }

        private void AllyGetCloserToEnemy(AIAlly ally)
        {
            ShowActionDebugLogs(ally.name + " Getting Closer To Rival");
            //Debug.Log(ally.name + " Getting Closer To Rival");

            AIEnemy targetEnemy = _aiEnemies[ally.GetContext().GetRivalIndex()];
            
            ally.ContinueNavigation();
            
            ally.SetDestination(targetEnemy.GetNavMeshAgentComponent().GetTransformComponent());
        }

        private void AllyAttack(AIAlly ally)
        {
            ShowActionDebugLogs(ally.name + " Attacking");
            //Debug.Log(ally.name + " Attacking");
            
            ally.Attack();
        }

        private void AllyFlee(AIAlly ally)
        {
            //TODO (REWORK) ALLY FLEE 
            
            ally.ContinueNavigation();
            
            EvaluateClosestPoint(ally);
            
            ShowActionDebugLogs(ally.name + " Fleeing");
            //Debug.Log(ally.name + " Fleeing");
        }

        private void AllyDodge(AIAlly ally)
        {
            List<Vector2> dangerZone = ally.GetOncomingEnemiesAttacksCorners();

            Vector3 position = ally.transform.position;

            Vector2 startPosition = new Vector2(position.x, position.z);
            Vector2 nearestPointToDodge;

            AIAllyContext context = ally.GetContext();

            float radius = context.GetRadius();

            Collider[] colliders;

            int layerMaskToSeek = (int)Math.Pow(2, GameManager.Instance.GetTerrainLayer());
            int layerMaskToAvoid = (int)Math.Pow(2, GameManager.Instance.GetEnemyAttackZone());

            List<AIEnemyAttackCollider> zonesToRebake = new List<AIEnemyAttackCollider>();
            
            do
            {
                colliders = Array.Empty<Collider>();
                
                nearestPointToDodge = GetNearestPointToDodge(dangerZone, startPosition, radius);

                colliders = Physics.OverlapCapsule(new Vector3(nearestPointToDodge.x, 0, nearestPointToDodge.y),
                    new Vector3(nearestPointToDodge.x, 1.5f, nearestPointToDodge.y), radius, layerMaskToSeek);

                if (colliders.Length == 0)
                {
                    continue;
                }

                colliders = Physics.OverlapCapsule(new Vector3(nearestPointToDodge.x, 0, nearestPointToDodge.y),
                    new Vector3(nearestPointToDodge.x, 1.5f, nearestPointToDodge.y), radius, layerMaskToAvoid);

                if (colliders.Length == 0)
                {
                    break;
                }

                List<Vector2> newZone = new List<Vector2>();
                    
                for (int i = 0; i < colliders.Length; i++)
                {
                    AIEnemyAttackCollider enemyAttackCollider = colliders[i].GetComponent<AIEnemyAttackCollider>();
                    
                    zonesToRebake.Add(enemyAttackCollider);
                    
                    newZone.AddRange(enemyAttackCollider.GetCornerPoints());

                    dangerZone = PolygonUtilities.Union2Polygons(dangerZone, newZone);
                    
                    newZone.Clear();
                }

            } while (true);
            
            ally.DodgeAttack(new VectorComponent(new Vector3(nearestPointToDodge.x, position.y, nearestPointToDodge.y)));

            foreach (AIEnemyAttackCollider enemyAttackCollider in zonesToRebake)
            {
                SubscribeToRebake(enemyAttackCollider);
            }
            
            ShowActionDebugLogs(ally.name + " Dodging");
            //Debug.Log(ally.name + " Dodging");
        }

        private void AllyHelpAnotherAlly(AIAlly ally)
        {
            //TODO HELP ANOTHER ALLY
            
            ally.ContinueNavigation();
            
            ShowActionDebugLogs(ally.name + " Helping another ally");
            //Debug.Log(ally.name + " Helping another ally");
        }

        #endregion
        
        #region Enemy

        private void EnemyPerformAction(AIEnemy enemy, AIEnemyAction enemyAction)
        {
            _aiEnemyActions[enemyAction](enemy);
        }

        private void EnemyPatrol(AIEnemy enemy)
        {
            //TODO ENEMY PATROL
            ShowActionDebugLogs(enemy.name + " Patrolling");
            //Debug.Log(enemy.name + " Patrolling");
        }

        private void EnemyRequestRival(AIEnemy enemy)
        {
            List<uint> visibleRivals = enemy.GetVisibleRivals();
            
            ShowActionDebugLogs(enemy.name + " Requesting Rival");
            //Debug.Log(enemy.name + " Requesting Rival");

            if (visibleRivals.Count == 0)
            {
                return;
            }

            uint targetId;

            NavMeshAgentComponent navMeshAgentComponent = enemy.GetNavMeshAgentComponent();

            if (visibleRivals.Count == 1)
            {
                targetId = visibleRivals[0];
            }
            else
            {
                targetId = GetClosestRival<AIAlly, AIAllyContext, AllyAttackComponent, DamageComponent>(
                    navMeshAgentComponent.GetTransformComponent(), _aiAllies, visibleRivals);
            }

            AIAlly targetAlly = _aiAllies[targetId];

            AIAllyContext targetAllyContext = targetAlly.GetContext();

            uint rivalIndex = targetAlly.GetCombatAgentInstance();
            
            enemy.SetRivalIndex(rivalIndex);
            enemy.SetHasATarget(true);
            enemy.SetRivalTransform(targetAllyContext.GetAgentTransform());
            
            MergeOverlappingThreatGroupsWithSameTarget(enemy, rivalIndex);
        }

        private void EnemyGetCloserToAlly(AIEnemy enemy)
        {
            ShowActionDebugLogs(enemy.name + " Getting Closer To Rival");
            //Debug.Log(enemy.name + " Getting Closer To Rival");

            AIAlly targetEnemy = _aiAllies[enemy.GetContext().GetRivalIndex()];
            
            enemy.ContinueNavigation();
            
            enemy.SetDestination(targetEnemy.GetNavMeshAgentComponent().GetTransformComponent());
        }

        private void EnemyAttack(AIEnemy enemy)
        {
            ShowActionDebugLogs(enemy.name + " Attacking");
            //Debug.Log(enemy.name + " Attacking");
            
            AttackComponent attackComponent = enemy.Attack();
            
            EnemyStartCastingAnAttack(enemy.transform, attackComponent, enemy);
        }

        private void EnemyFlee(AIEnemy enemy)
        {
            //TODO ENEMY FLEE
            
            enemy.ContinueNavigation();
            
            ShowActionDebugLogs(enemy.name + " Fleeing");
            //Debug.Log(enemy.name + " Fleeing");
        }

        #endregion

        #endregion

        #region Add Combat Agent

        public void AddAIAlly(AIAlly aiAlly)
        {
            AddAlly(aiAlly);
        }

        private void AddAlly(AIAlly aiAlly)
        {
            //TODO MORAL SYSTEM

            _aiAllies.Add(aiAlly.GetCombatAgentInstance(), aiAlly);
        }

        public void AddAIEnemy(AIEnemy aiEnemy, AIEnemyContext aiEnemyContext)
        {
            AddEnemy(aiEnemy);

            uint combatInstanceID = aiEnemy.GetCombatAgentInstance();
            
            foreach (uint threatGroupIndex in _enemiesIndexesInsideThreatGroup.Keys)
            {
                if (!_enemiesIndexesInsideThreatGroup[threatGroupIndex].Contains(combatInstanceID))
                {
                    continue;
                }

                aiEnemyContext.SetCurrentThreatGroup(threatGroupIndex);
                break;
            }
            
            AddEnemyAttack(aiEnemy.GetAttackComponents(), GameManager.Instance.GetAllyLayer());
        }

        private void AddEnemy(AIEnemy aiEnemy)
        {
            _enemiesOfTheSameThreatGroupOverlappingTriggers.Add(aiEnemy, new List<uint>());

            AddThreat(aiEnemy.GetCombatAgentInstance(), aiEnemy.GetThreatComponent(), 
                aiEnemy.GetNavMeshAgentComponent().GetTransformComponent());
            
            _aiEnemies.Add(aiEnemy.GetCombatAgentInstance(), aiEnemy);
        }

        private void AddEnemyAttack(List<AttackComponent> attackComponents, int layerTarget)
        {
            foreach (AttackComponent attackComponent in attackComponents)
            {
                GameObject colliderObject = null;

                switch (attackComponent.GetAIAttackAoEType())
                {
                    case AIAttackAoEType.RECTANGLE_AREA:
                        colliderObject = Instantiate(_enemyRectangleAttackColliderPrefab);
                        AIEnemyRectangleAttackCollider enemyRectangleAttackCollider = 
                            colliderObject.GetComponent<AIEnemyRectangleAttackCollider>();
                        
                        enemyRectangleAttackCollider.SetRectangleAttackComponent((RectangleAttackComponent)attackComponent);
                        enemyRectangleAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _enemiesAttacksColliders.Add(attackComponent, enemyRectangleAttackCollider);
                        break;

                    case AIAttackAoEType.CIRCLE_AREA:
                        colliderObject = Instantiate(_enemyCircleAttackColliderPrefab);
                        AIEnemyCircleAttackCollider enemyCircleAttackCollider = colliderObject.GetComponent<AIEnemyCircleAttackCollider>();
                        enemyCircleAttackCollider.SetCircleAttackComponent((CircleAttackComponent)attackComponent);
                        enemyCircleAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _enemiesAttacksColliders.Add(attackComponent, enemyCircleAttackCollider);
                        break;

                    case AIAttackAoEType.CONE_AREA:
                        AIEnemyConeAttackCollider enemyConeAttackCollider = colliderObject.GetComponent<AIEnemyConeAttackCollider>();
                        enemyConeAttackCollider.SetConeAttackComponent((ConeAttackComponent)attackComponent);
                        enemyConeAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _enemiesAttacksColliders.Add(attackComponent, enemyConeAttackCollider);
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

        public void OnEnemyJoinEnemy(AIEnemy aiEnemy, uint otherEnemyID)
        {
            AIEnemy otherEnemy = _aiEnemies[otherEnemyID];

            if (aiEnemy.GetContext().GetRivalIndex() != otherEnemy.GetContext().GetRivalIndex())
            {
                return;
            }
            
            _enemiesOfTheSameThreatGroupOverlappingTriggers[aiEnemy].Add(otherEnemyID);

            uint aiEnemyThreatGroup = aiEnemy.GetThreatComponent().currentThreatGroup;
            uint otherEnemyThreatGroup = otherEnemy.GetThreatComponent().currentThreatGroup;

            if (aiEnemyThreatGroup < otherEnemyThreatGroup)
            {
                MergeThreatGroups(otherEnemyThreatGroup, aiEnemyThreatGroup);
                return;
            }

            MergeThreatGroups(aiEnemyThreatGroup, otherEnemyThreatGroup);
        }

        private void MergeOverlappingThreatGroupsWithSameTarget(AIEnemy enemy, uint rivalIndex)
        {
            List<uint> overlappingThreatGroupsWithSameTarget =
                GetOverlappingThreatGroupsWithSameTarget(rivalIndex, enemy.GetOverlappingEnemies());

            uint enemyOriginalThreatGroup = enemy.GetThreatComponent().GetOriginalThreatGroup();
            
            if (overlappingThreatGroupsWithSameTarget.Count == 0)
            {
                _threatGroups[enemyOriginalThreatGroup].Key.groupTarget = rivalIndex;
                return;
            }
            
            overlappingThreatGroupsWithSameTarget.Add(enemyOriginalThreatGroup);
            
            overlappingThreatGroupsWithSameTarget.Sort();

            _threatGroups[overlappingThreatGroupsWithSameTarget[0]].Key.groupTarget = rivalIndex;

            for (int i = overlappingThreatGroupsWithSameTarget.Count - 1; i >= 1; i--)
            {
                MergeThreatGroups(overlappingThreatGroupsWithSameTarget[i], 
                    overlappingThreatGroupsWithSameTarget[i - 1]);
            }
        }

        private List<uint> GetOverlappingThreatGroupsWithSameTarget(uint targetID, List<uint> enemiesIDs)
        {
            List<uint> threatGroups = new List<uint>();
            List<uint> threatGroupsChecked = new List<uint>();

            foreach (uint enemyID in enemiesIDs)
            {
                AIEnemy enemy = _aiEnemies[enemyID];

                uint currentThreatGroup = enemy.GetContext().GetCurrentThreatGroup();

                if (threatGroupsChecked.Contains(currentThreatGroup))
                {
                    continue;
                }
                
                threatGroupsChecked.Add(currentThreatGroup);
                
                if (enemy.GetContext().GetRivalIndex() == targetID)
                {
                    continue;
                }
                
                threatGroups.Add(currentThreatGroup);
            }

            return threatGroups;
        }

        private void MergeThreatGroups(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            MoveWholeThreatGroupToAnotherThreatGroup(threatGroupFromWhichTheyCome, threatGroupToMove);

            for (int i = _enemiesIndexesInsideThreatGroup[threatGroupFromWhichTheyCome].Count - 1; i >= 0; i--)
            {
                uint enemyIndex = _enemiesIndexesInsideThreatGroup[threatGroupFromWhichTheyCome][i];
                
                _enemiesIndexesInsideThreatGroup[threatGroupFromWhichTheyCome].Remove(enemyIndex);
                _enemiesIndexesInsideThreatGroup[threatGroupToMove].Add(enemyIndex);
                _aiEnemies[enemyIndex].SetCurrentThreatGroup(threatGroupToMove);
            }
            
            UpdateThreatInAllyContext(threatGroupFromWhichTheyCome, threatGroupToMove);
        }

        public void OnEnemySeparateFromEnemy(AIEnemy aiEnemy, uint otherEnemyID)
        {
            AIEnemy otherEnemy = _aiEnemies[otherEnemyID];
            
            _enemiesOfTheSameThreatGroupOverlappingTriggers[aiEnemy].Remove(otherEnemyID);

            List<AIEnemy> allContacts = GetAllContacts(aiEnemy);

            List<ThreatComponent> threatComponents = new List<ThreatComponent>();

            foreach (AIEnemy aiEnemyInContact in allContacts)
            {
                threatComponents.Add(aiEnemyInContact.GetThreatComponent());
            }

            uint lowestThreatGroup = FindLowestThreatGroupIndex(threatComponents);
            uint otherThreatGroup = otherEnemy.GetThreatComponent().currentThreatGroup;

            if (lowestThreatGroup == otherThreatGroup)
            {
                return;
            }

            MoveGivenThreatsToAnotherThreatGroup(threatComponents, otherThreatGroup,
                lowestThreatGroup);

            foreach (AIEnemy enemy in allContacts)
            {
                uint combatAgentInstance = enemy.GetCombatAgentInstance();
                _enemiesIndexesInsideThreatGroup[otherThreatGroup].Remove(combatAgentInstance);
                _enemiesIndexesInsideThreatGroup[lowestThreatGroup].Add(combatAgentInstance);
                enemy.SetCurrentThreatGroup(lowestThreatGroup);
            }

            UpdateThreatInAllyContext(otherThreatGroup, lowestThreatGroup);
        }

        private void UpdateThreatInAllyContext(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            foreach (AIAlly ally in _aiAllies.Values)
            {
                if (ally.GetContext().GetThreatGroupOfTarget() != threatGroupFromWhichTheyCome)
                {
                    continue;
                }

                ally.SetThreatGroupOfTarget(threatGroupToMove);
                ally.SetThreatWeightOfTarget(_threatGroups[threatGroupToMove].Key.threatGroupWeight);
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

                foreach (uint aiEnemyIDToCheck in _enemiesOfTheSameThreatGroupOverlappingTriggers[currentEnemy])
                {
                    AIEnemy aiEnemyToCheck = _aiEnemies[aiEnemyIDToCheck];
                    
                    if (!contacts.Contains(aiEnemyToCheck))
                    {
                        aiEnemiesStack.Push(aiEnemyToCheck);
                    }
                }
            }

            return contacts;
        }

        private uint FindLowestThreatGroupIndex(List<ThreatComponent> threatComponents)
        {
            uint lowestThreatGroupIndex = threatComponents[0].GetOriginalThreatGroup();

            foreach (ThreatComponent threatComponent in threatComponents)
            {
                uint currentOriginalThreatGroupIndex = threatComponent.GetOriginalThreatGroup();

                if (lowestThreatGroupIndex < currentOriginalThreatGroupIndex)
                {
                    continue;
                }

                lowestThreatGroupIndex = currentOriginalThreatGroupIndex;
            }

            return lowestThreatGroupIndex;
        }

        private List<uint> GetPossibleRivals(List<uint> visibleRivals, List<uint> threatGroupsToAvoid, float moralWeight)
        {
            for (int i = visibleRivals.Count - 1; i >= 0; i--)
            {
                uint enemyID = visibleRivals[i];

                AIEnemyContext enemyContext = _aiEnemies[enemyID].GetContext();

                if (!threatGroupsToAvoid.Contains(enemyContext.GetCurrentThreatGroup()))
                {
                    continue;
                }
                
                visibleRivals.RemoveAt(i);
            }

            return visibleRivals;
        }

        private uint GetClosestRival<TAgent, TRivalContext, TRivalAttackComponent, TRivalDamageComponent>(
            IPosition positionComponent, Dictionary<uint, TAgent> rivalsDictionary, List<uint> possibleTargetsAICombatAgentIDs)
        
            where TAgent : AICombatAgentEntity<TRivalContext, TRivalAttackComponent, TRivalDamageComponent>
            where TRivalContext : AICombatAgentContext
            where TRivalAttackComponent : AttackComponent
            where TRivalDamageComponent : DamageComponent
        {
            uint targetID = 0;
            TAgent currentTarget;

            float targetDistance = 300000000;
            float currentTargetDistance;

            for (int i = 0; i < possibleTargetsAICombatAgentIDs.Count; i++)
            {
                currentTarget = rivalsDictionary[possibleTargetsAICombatAgentIDs[i]];

                currentTargetDistance = (currentTarget.transform.position - positionComponent.GetPosition()).magnitude;

                if (currentTargetDistance >= targetDistance)
                {
                    continue;
                }

                targetID = currentTarget.GetCombatAgentInstance();
                targetDistance = currentTargetDistance;
            }

            return targetID;
        }
        
        public void OnAllyDefeated(AIAlly aiAlly)
        {
            //TODO ON ALLY DEFEATED
            
            OnAgentDefeated<AIEnemy, AIEnemyContext, AttackComponent, AllyDamageComponent, 
                AIAllyContext, AllyAttackComponent, DamageComponent>(aiAlly, ref _aiEnemies);
        }

        public void OnEnemyReceiveDamage(uint enemyAgentInstanceID, uint enemyHealth, float enemyStress, bool isStunned)
        {
            foreach (AIAlly ally in _aiAllies.Values)
            {
                if (ally.GetContext().GetRivalIndex() != enemyAgentInstanceID)
                {
                    continue;
                }

                ally.SetEnemyHealth(enemyHealth);
                ally.SetEnemyCurrentStress(enemyStress);
                ally.SetIsEnemyStunned(isStunned);
            }
        }

        public void OnEnemyStunEnds(uint enemyAgentInstanceID)
        {
            foreach (AIAlly ally in _aiAllies.Values)
            {
                if (ally.GetContext().GetRivalIndex() != enemyAgentInstanceID)
                {
                    continue;
                }
                
                ally.SetIsEnemyStunned(false);
            }
        }

        public void OnEnemyDefeated(AIEnemy aiEnemy)
        {
            uint combatAgentInstance = aiEnemy.GetCombatAgentInstance();
            
            EraseThreat(combatAgentInstance, aiEnemy.GetThreatComponent(), aiEnemy.GetNavMeshAgentComponent().GetTransformComponent());

            List<uint> enemiesOverlapping = _enemiesOfTheSameThreatGroupOverlappingTriggers[aiEnemy];

            for (int i = enemiesOverlapping.Count - 1; i >= 0; i--)
            {
                OnEnemySeparateFromEnemy(_aiEnemies[enemiesOverlapping[i]], aiEnemy.GetCombatAgentInstance());
            }

            _aiEnemies.Remove(combatAgentInstance);
            
            OnAgentDefeated<AIAlly, AIAllyContext, AllyAttackComponent, DamageComponent, 
                AIEnemyContext, AttackComponent, AllyDamageComponent>(aiEnemy, ref _aiAllies);
        }

        private void OnAgentDefeated<TAgent, TRivalContext, TRivalAttackComponent, TRivalDamageComponent, 
            TOwnContext, TOwnAttackComponent, TOwnDamageComponent>(
            AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent> aiCombatAgentDefeated, 
            ref Dictionary<uint, TAgent> agents)
        
            where TAgent : AICombatAgentEntity<TRivalContext, TRivalAttackComponent, TRivalDamageComponent>
            where TRivalContext : AICombatAgentContext
            where TRivalAttackComponent : AttackComponent
            where TRivalDamageComponent : DamageComponent
            where TOwnContext : AICombatAgentContext
            where TOwnAttackComponent : AttackComponent
            where TOwnDamageComponent : DamageComponent
        {
            foreach (TAgent agent in agents.Values)
            {
                if (agent.GetContext().GetRivalIndex() != aiCombatAgentDefeated.GetCombatAgentInstance())
                {
                    continue;
                }
                
                agent.GetContext().SetHasATarget(false);
                ECSNavigationManager.Instance.UpdateNavMeshAgentTransformDestination(agent.GetNavMeshAgentComponent(), null);
            }
        }

        #endregion

        #region Flee Events

        public void RequestSafeSpot(AICombatAgentEntity<AIAllyContext, AllyAttackComponent, DamageComponent> aiCombatAgentEntity)
        {
            
        }

        #endregion

        #region Attack Events

        private void EnemyStartCastingAnAttack(Transform attackerTransform, 
            AttackComponent attackComponent, AIEnemy enemy)
        {
            if (attackComponent.IsOnCooldown())
            {
                enemy.GetContext().SetIsAttacking(false);
                return;
            }
            
            AIAttackCollider attackCollider = _enemiesAttacksColliders[attackComponent];
            attackCollider.SetParent(attackerTransform);
            attackCollider.gameObject.SetActive(true);
            StartCoroutine(StartEnemyAttackCastTimeCoroutine(attackComponent, attackCollider, enemy));
        }

        private void PutAttackOnCooldown(AttackComponent attackComponent, AIEnemy enemy)
        {
            StartCoroutine(StartCooldownCoroutine(attackComponent, enemy));
        }

        #endregion

        #region Systems

        #region Moral System

        private void IncreaseMoralLevel(ref MoralComponent firstMoralComponent, ref MoralComponent secondMoralComponent)
        {
            firstMoralComponent.AddMinMoralWeight();
            secondMoralComponent.AddMinMoralWeight();
        }

        private void DecreaseMoralLevel(ref MoralComponent firstMoralComponent, ref MoralComponent secondMoralComponent)
        {
            firstMoralComponent.SubtractMinMoralWeight();
            secondMoralComponent.SubtractMinMoralWeight();
        }

        private bool EvaluateConfrontation(IStatWeight moralComponent, IStatWeight threatComponent)
        {
            return moralComponent.GetWeight() > threatComponent.GetWeight();
        }

        #endregion

        #region Threat System

        private void UpdateThreatGroupsBarycenter()
        {
            for (uint i = 1; i < _groupThreatsComponents.Count + 1; i++)
            {
                List<TransformComponent> transformComponents = _groupThreatsComponents[i].Value;
                if (transformComponents.Count == 0)
                {
                    continue;
                }
                VectorComponent vectorComponent = ReturnThreatBarycenter(transformComponents);
                _threatGroups[i].Value.SetPosition(vectorComponent.GetPosition());
            }
        }

        private VectorComponent ReturnThreatBarycenter(List<TransformComponent> transformComponents)
        {
            Vector3 XZposition = new Vector3();

            foreach (TransformComponent transformComponent in transformComponents)
            {
                XZposition += transformComponent.GetTransform().position;
            }

            XZposition /= transformComponents.Count;

            return new VectorComponent(XZposition);
        }

        private void UpdateThreatGroupsRadius()
        {
            foreach (KeyValuePair<uint, List<uint>> enemiesInsideThreatGroup in _enemiesIndexesInsideThreatGroup)
            {
                List<uint> enemiesIDs = enemiesInsideThreatGroup.Value;

                if (enemiesIDs.Count == 0)
                {
                    continue;
                }
                
                uint threatGroupIndex = enemiesInsideThreatGroup.Key;

                Vector3 threatGroupBarycenter = _threatGroups[threatGroupIndex].Value.GetPosition();

                AIEnemy farthestEnemyFromTheBarycenter = _aiEnemies[enemiesIDs[0]];

                float farthestEnemyDistanceToBarycenter =
                    (threatGroupBarycenter - farthestEnemyFromTheBarycenter.transform.position).magnitude;

                for (int i = 1; i < enemiesIDs.Count; i++)
                {
                    AIEnemy currentEnemy = _aiEnemies[enemiesIDs[i]];

                    float currentEnemyDistanceToBarycenter =
                        (threatGroupBarycenter - currentEnemy.transform.position).magnitude;

                    if (currentEnemyDistanceToBarycenter < farthestEnemyDistanceToBarycenter)
                    {
                        continue;
                    }

                    farthestEnemyFromTheBarycenter = currentEnemy;
                    farthestEnemyDistanceToBarycenter = currentEnemyDistanceToBarycenter;
                }

                _threatGroups[threatGroupIndex].Key.groupRadius =
                    farthestEnemyDistanceToBarycenter + 
                    farthestEnemyFromTheBarycenter.GetContext().GetOriginalThreatGroupInfluenceRadius();
            }
        }

        private void AddThreat(uint combatAgentIndex, ThreatComponent threatComponent, TransformComponent transformComponent)
        {
            uint threatGroup = threatComponent.GetOriginalThreatGroup();

            List<uint> enemiesIndexes = new List<uint>
            {
                combatAgentIndex
            };
            
            List<ThreatComponent> threatComponents = new List<ThreatComponent>
            {
                threatComponent
            };
            
            List<TransformComponent> transformComponents = new List<TransformComponent>
            {
                transformComponent
            };
            
            _enemiesIndexesInsideThreatGroup.Add(threatGroup, enemiesIndexes);
            
            _groupThreatsComponents.Add(threatGroup, 
                new KeyValuePair<List<ThreatComponent>, List<TransformComponent>>(threatComponents, transformComponents));

            ThreatGroupComponent threatGroupComponent = new ThreatGroupComponent(0);
            VectorComponent vectorComponent = new VectorComponent(new Vector3());
            
            _threatGroups.Add(threatGroup, 
                new KeyValuePair<ThreatGroupComponent, VectorComponent>(threatGroupComponent, vectorComponent));
            
            _threatGroups[threatGroup].Key.threatGroupWeight = threatComponent.GetWeight();
        }

        private void EraseThreat(uint combatAgentInstance, ThreatComponent threatComponent, TransformComponent transformComponent)
        {
            uint originalThreatGroup = threatComponent.GetOriginalThreatGroup();
            uint currentThreatGroup = threatComponent.GetCurrentGroup();

            _enemiesIndexesInsideThreatGroup[currentThreatGroup].Remove(combatAgentInstance);
            _threatGroups[currentThreatGroup].Key.threatGroupWeight -= threatComponent.GetWeight();
            _groupThreatsComponents[currentThreatGroup].Key.Remove(threatComponent);
            _groupThreatsComponents[currentThreatGroup].Value.Remove(transformComponent);

            _groupThreatsComponents.Remove(originalThreatGroup);
        }

        private void MoveWholeThreatGroupToAnotherThreatGroup(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            for (int i = 0; i < _groupThreatsComponents[threatGroupFromWhichTheyCome].Key.Count; i++)
            {
                MoveSingleThreatToAnotherThreatGroup(_groupThreatsComponents[threatGroupFromWhichTheyCome].Key[i], 
                    threatGroupFromWhichTheyCome, threatGroupToMove);
            }
        }

        private void MoveGivenThreatsToAnotherThreatGroup(List<ThreatComponent> threatComponentsToMove, 
            uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            foreach (ThreatComponent threatComponent in threatComponentsToMove)
            {
                MoveSingleThreatToAnotherThreatGroup(threatComponent, threatGroupFromWhichTheyCome, threatGroupToMove);
            }
        }

        private void MoveSingleThreatToAnotherThreatGroup(ThreatComponent threatComponent, uint threatGroupFromWhichComes, 
            uint threatGroupToMove)
        {
            int threatComponentListIndex = _groupThreatsComponents[threatGroupFromWhichComes].Key.IndexOf(threatComponent);

            if (threatComponentListIndex == -1)
            {
                return;
            }
            
            MoveThreatComponentToThreatGroup(_groupThreatsComponents[threatGroupFromWhichComes].Key[threatComponentListIndex],
                threatGroupFromWhichComes, threatGroupToMove);
                    
            MoveTransformComponentToThreatGroup(_groupThreatsComponents[threatGroupFromWhichComes].Value[threatComponentListIndex],
                threatGroupFromWhichComes, threatGroupToMove);
        }

        private void MoveThreatComponentToThreatGroup(ThreatComponent threatComponent, uint threatGroupFromWhichTheyCome, 
            uint threatGroupToMove)
        {
            _groupThreatsComponents[threatGroupFromWhichTheyCome].Key.Remove(threatComponent);
            _threatGroups[threatGroupFromWhichTheyCome].Key.threatGroupWeight -=
                threatComponent.GetWeight();
            
            threatComponent.currentThreatGroup = threatGroupToMove;
            _groupThreatsComponents[threatGroupToMove].Key.Add(threatComponent);
            _threatGroups[threatGroupToMove].Key.threatGroupWeight +=
                threatComponent.GetWeight();
        }

        private void MoveTransformComponentToThreatGroup(TransformComponent transformComponent, 
            uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            _groupThreatsComponents[threatGroupFromWhichTheyCome].Value.Remove(transformComponent);
            
            _groupThreatsComponents[threatGroupToMove].Value.Add(transformComponent);
        }

        #endregion
        
        #region Combat System

        private void UpdateDistanceToRival<TOwnContext, TOwnAttackComponent, TOwnDamageComponent>(
            AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent> combatAgent)
            
            where TOwnContext : AICombatAgentContext
            where TOwnAttackComponent : AttackComponent
            where TOwnDamageComponent : DamageComponent
        {
            Vector3 vectorToRival = combatAgent.GetContext().GetRivalTransform().position - combatAgent.transform.position;
            
            combatAgent.SetVectorToRival(vectorToRival);
            combatAgent.SetDistanceToRival(vectorToRival.magnitude);
        }

        #endregion
        
        #region Attack System

        private IEnumerator StartEnemyAttackCastTimeCoroutine(AttackComponent attackComponent, AIAttackCollider attackCollider, 
            AIEnemy enemy)
        {
            attackComponent.StartCastTime();
            while (attackComponent.IsCasting() && !enemy.GetContext().IsStunned())
            {
                attackComponent.DecreaseCurrentCastTime();
                yield return null;
            }

            if (!enemy.GetContext().IsStunned())
            {
                attackCollider.StartInflictingDamage();

                if (attackComponent.DoesDamageOverTime())
                {
                    StartCoroutine(StartDamageOverTime(attackComponent, attackCollider, enemy));
                    yield break;
                }
            
                enemy.RotateToNextPathCorner();
            }
            
            Instance.PutAttackOnCooldown(attackComponent, enemy);
            attackCollider.Deactivate();
        }

        private IEnumerator StartDamageOverTime(AttackComponent attackComponent, AIAttackCollider attackCollider, 
            AIEnemy enemy)
        {
            while (attackComponent.DidDamageOverTimeFinished())
            {
                attackComponent.DecreaseRemainingTimeDealingDamage();
                yield return null;
            }
           
            enemy.RotateToNextPathCorner();
            Instance.PutAttackOnCooldown(attackComponent, enemy);
            attackCollider.Deactivate();
        }

        private IEnumerator StartCooldownCoroutine(AttackComponent attackComponent, AIEnemy enemy)
        {
            attackComponent.StartCooldown();
            while (attackComponent.IsOnCooldown())
            {
                attackComponent.DecreaseCooldown();
                yield return null;
            }
            
            enemy.OnAttackAvailableAgain(attackComponent);
        }
        
        #endregion
        
        #region Flee System
        
        //ERASE!!!!
        private List<Vector3> GetTerrainPositions(List<GameObject> FLEE_POINTS)
        {
            List<Vector3> points = new List<Vector3>();
            
            RaycastHit hit;

            foreach (GameObject gameObject in FLEE_POINTS)
            {
                Ray ray = new Ray(gameObject.transform.position, Vector3.down);
                if (Physics.Raycast(ray, out hit))
                {
                    points.Add(hit.point);
                }
            }

            return points;
        }
        
        private void EvaluateClosestPoint(AIAlly combatAgentNeedsToFlee)
        {
            if (FLEE_POINTS_RECORD.ContainsKey(combatAgentNeedsToFlee))
            {
                return;
            }
            
            Vector3 agentPosition = combatAgentNeedsToFlee.transform.position;
         
            Vector3 destination = TERRAIN_POSITIONS[0];            
            float closestDistance = (destination - agentPosition).magnitude;

            int index = 0;

            for (int i = 0; i < TERRAIN_POSITIONS.Count; i++)
            {
                Vector3 currentPosition = TERRAIN_POSITIONS[i];
                
                float currentDistance = (agentPosition - currentPosition).magnitude;
                if (currentDistance > closestDistance)
                {
                    continue;
                }

                closestDistance = currentDistance;
                destination = currentPosition;
                index = i;
            }
            
            FLEE_POINTS_RECORD.Add(combatAgentNeedsToFlee, index);
            
            combatAgentNeedsToFlee.SetDestination(new VectorComponent(destination));
        } 

        private IEnumerator UpdateFleeMovement()
        {
            while (true)
            {
                Dictionary<AIAlly, int> newIndexes = new Dictionary<AIAlly, int>(); 
                
                foreach (var combatAgentFleeing in FLEE_POINTS_RECORD)
                {
                    AIAlly combatAgent = combatAgentFleeing.Key;
                    int index = combatAgentFleeing.Value;

                    if ((combatAgent.transform.position - TERRAIN_POSITIONS[index]).magnitude < 8)
                    {
                        int newIndex = (combatAgentFleeing.Value + 1) % TERRAIN_POSITIONS.Count;
                        
                        newIndexes.Add(combatAgent, newIndex);
                    }
                }

                foreach (var VARIABLE in newIndexes)
                {
                    AIAlly combatAgent = VARIABLE.Key;
                    int newIndex = VARIABLE.Value;

                    FLEE_POINTS_RECORD[combatAgent] = newIndex;
                    
                    combatAgent.SetDestination(new VectorComponent(TERRAIN_POSITIONS[newIndex]));
                }

                yield return null;
            }
        }
        //

        #endregion

        #region Dodge System

        private Vector2 GetNearestPointToDodge(List<Vector2> polygon, Vector2 point, float radius)
        {
            Vector2 closestPoint = Vector2.zero;
            float shortestDistance = Mathf.Infinity;

            Vector2 start;
            Vector2 end;
            Vector2 direction = new Vector2();
            Vector2 closestPointOnEdge = new Vector2();

            for (int i = 0; i < polygon.Count; i++)
            {
                start = polygon[i];
                end = polygon[(i + 1) % polygon.Count];

                direction = end - start;
                
                closestPointOnEdge = GetClosestPointOnLineSegment(start, end, point);

                float distance = Vector2.Distance(point, closestPointOnEdge);

                if (distance >= shortestDistance)
                {
                    continue;
                }
                
                shortestDistance = distance;
                closestPoint = closestPointOnEdge + (new Vector2(-direction.y, direction.x) * (radius * 1.5f));
            }

            return closestPoint;
        }
        
        private Vector2 GetClosestPointOnLineSegment(Vector2 start, Vector2 end, Vector2 point)
        {
            Vector2 line = end - start;
            float lengthSquared = line.sqrMagnitude;

            float distance = Vector2.Dot(point - start, line) / lengthSquared;
            distance = Mathf.Clamp01(distance);

            return start + distance * line;
        }

        public void SubscribeToRebake(AIEnemyAttackCollider enemyAttackCollider)
        {
            _enemyAttackCollidersSubscribedToRebakeAllyNavMesh.Add(enemyAttackCollider);
            
            if (_rebakeCoroutine != null)
            {
                return;
            }

            _rebakeCoroutine = StartCoroutine(RebakeNavMeshSurfaceCoroutine());
        }

        private void UnsubscribeToRebake(AIEnemyAttackCollider enemyAttackCollider)
        {
            _enemyAttackCollidersSubscribedToRebakeAllyNavMesh.Remove(enemyAttackCollider);
            _allyNavMeshSurface.BuildNavMesh();

            if (_enemyAttackCollidersSubscribedToRebakeAllyNavMesh.Count != 0)
            {
                return;
            }
            
            StopCoroutine(_rebakeCoroutine);
            _rebakeCoroutine = null;
        }

        private IEnumerator RebakeNavMeshSurfaceCoroutine()
        {
            while (true)
            {
                for (int i = _enemyAttackCollidersSubscribedToRebakeAllyNavMesh.Count - 1; i >= 0; i--)
                {
                    AIEnemyAttackCollider enemyAttackCollider = _enemyAttackCollidersSubscribedToRebakeAllyNavMesh[i];
                    
                    if (!enemyAttackCollider.gameObject.activeSelf)
                    {
                        _allyNavMeshSurface.BuildNavMesh();
                        UnsubscribeToRebake(enemyAttackCollider);                        
                        continue;
                    }

                    if (!enemyAttackCollider.IsWalkable())
                    {
                        continue;
                    }

                    if (enemyAttackCollider.HasCombatAgentsTriggering())
                    {
                        continue;
                    }

                    enemyAttackCollider.SetNotWalkable();
                    _allyNavMeshSurface.BuildNavMesh();
                }
                
                yield return null;    
            }
        }

        #endregion
        
        #endregion

        private List<TAgent> ReturnAllDictionaryValuesInAList<TAgent, TOwnContext, TOwnAttackComponent, TOwnDamageComponent>(
            Dictionary<uint, TAgent> agentsDictionary)
        
            where TAgent : AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent>
            where TOwnContext : AICombatAgentContext
            where TOwnAttackComponent : AttackComponent
            where TOwnDamageComponent : DamageComponent
        {
            List<TAgent> agentsList = new List<TAgent>();

            foreach (TAgent combatAgent in agentsDictionary.Values)
            {
                agentsList.Add(combatAgent);
            }

            return agentsList;
        }

        private List<AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent>> 
            ReturnAllRivals<TAgent, TOwnContext, TOwnAttackComponent, TOwnDamageComponent>(AIAgentType aiAgentType)
        
            where TAgent : AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent>
            where TOwnContext : AICombatAgentContext
            where TOwnAttackComponent : AttackComponent
            where TOwnDamageComponent : DamageComponent
        {
            List<AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent>> combatAgents = 
                new List<AICombatAgentEntity<TOwnContext, TOwnAttackComponent, TOwnDamageComponent>>();

            for (AIAgentType i = 0; i < AIAgentType.ENUM_SIZE; i++)
            {
                if (aiAgentType == i)
                {
                    continue;
                }

                if (!_returnTheSameAgentsType.ContainsKey(i))
                {
                    continue;
                }

                List<TAgent> currentCombatAgents = ExecuteDelegate<List<TAgent>>(i);

                if (currentCombatAgents != null)
                {
                    combatAgents.AddRange(currentCombatAgents);    
                }
            }

            return combatAgents;
        }
        
        private T ExecuteDelegate<T>(AIAgentType agentType)
        {
            Delegate del = _returnTheSameAgentsType[agentType];
            
            if (del is Func<T> func)
            {
                return func();
            }
            return default;
        }

        private List<T> UnifyArraysInAList<T>(T[] firstArray, T[] secondArray)
        {
            List<T> list = new List<T>();
            
            list.AddRange(firstArray);

            foreach (T t in secondArray)
            {
                if (list.Contains(t))
                {
                    continue;
                }
                
                list.Add(t);
            }

            return list;
        }

        private List<T> UnifyLists<T>(List<T> firstList, List<T> secondList)
        {
            foreach (T t in secondList)
            {
                if (firstList.Contains(t))
                {
                    continue;
                }    
                firstList.Add(t);
            }
            
            return firstList;
        }

        private List<T> SubtractLists<T>(List<T> firstList, List<T> secondList)
        {
            foreach (T t in secondList)
            {
                if (!firstList.Contains(t))
                {
                    continue;
                }

                firstList.Remove(t);
            }
            
            return firstList;
        }
    }
}