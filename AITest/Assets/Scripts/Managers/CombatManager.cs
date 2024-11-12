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
using Random = UnityEngine.Random;

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
            { AIAllyAction.ROTATE , ally => ally.Rotate()},
            { AIAllyAction.ATTACK , ally => Instance.AllyAttack(ally)},
            { AIAllyAction.FLEE , ally => Instance.AllyFlee(ally)},
            { AIAllyAction.DODGE_ATTACK , ally => Instance.AllyDodge(ally)},
            { AIAllyAction.HELP_ANOTHER_ALLY , ally => Instance.AllyHelpAnotherAlly(ally)}
        };
        
        private Dictionary<AIEnemyAction, Action<AIEnemy>> _aiEnemyActions = new Dictionary<AIEnemyAction, Action<AIEnemy>>
        {
            { AIEnemyAction.PATROL , enemy => Instance.EnemyPatrol(enemy)},
            { AIEnemyAction.CHOOSE_NEW_RIVAL , enemy => Instance.EnemyRequestRival(enemy)},
            { AIEnemyAction.GET_CLOSER_TO_RIVAL , enemy => Instance.EnemyGetCloserToAlly(enemy)},
            { AIEnemyAction.ROTATE , enemy => enemy.Rotate()},
            { AIEnemyAction.ATTACK , enemy => Instance.EnemyAttack(enemy)},
            { AIEnemyAction.FLEE , enemy => Instance.EnemyFlee(enemy)}
        };

        private Dictionary<AttackComponent, AIEnemyAttackCollider> _enemiesAttacksColliders =
            new Dictionary<AttackComponent, AIEnemyAttackCollider>();

        private Dictionary<uint, List<uint>> _alliesIDsInsideMoralGroup = new Dictionary<uint, List<uint>>();

        private Dictionary<uint, KeyValuePair<List<MoralComponent>, List<TransformComponent>>> _groupMoralsComponents =
            new Dictionary<uint, KeyValuePair<List<MoralComponent>, List<TransformComponent>>>();

        private Dictionary<uint, KeyValuePair<MoralGroupComponent, VectorComponent>> _moralGroups =
            new Dictionary<uint, KeyValuePair<MoralGroupComponent, VectorComponent>>();

        private Dictionary<uint, List<uint>> _enemiesOfTheSameThreatGroupOverlappingTriggers =
            new Dictionary<uint, List<uint>>();

        private Dictionary<uint, List<uint>> _enemiesIDsInsideThreatGroup = new Dictionary<uint, List<uint>>();

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

        private void ShowActionDebugLogs(string message)
        {
            if (!_showActionsDebugLogs)
            {
                return;
            }
            
            Debug.Log(message);
        }
        //

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

        public List<uint> FilterThreatGroupsThatThreatMyMoralGroup(uint groupID, IStatWeight moralWeightComponent,
            uint threatGroupOfCurrentTarget, List<uint> visibleRivalsIDs)
        {
            List<uint> threatGroupsThatThreatMe = new List<uint>();
            List<uint> threatGroupsChecked = new List<uint>();

            foreach (uint enemyID in visibleRivalsIDs)
            {
                uint currentThreatGroupID = _aiEnemies[enemyID].GetContext().GetCurrentGroup();

                if (threatGroupsChecked.Contains(currentThreatGroupID))
                {
                    continue;
                }
                
                threatGroupsChecked.Add(currentThreatGroupID);

                ThreatGroupComponent threatGroupComponent = _threatGroups[currentThreatGroupID].Key;

                if (threatGroupComponent.GetGroupTarget() != groupID)
                {
                    continue;
                }

                float totalThreatWeight = threatGroupComponent.groupWeight;

                if (_threatGroups.ContainsKey(threatGroupOfCurrentTarget))
                {
                    totalThreatWeight += _threatGroups[threatGroupOfCurrentTarget].Key.groupWeight * 
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

                whichThreatGroupsAreAlliesFighting[counter] = ally.GetContext().GetRivalGroupIDOfTarget();
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

            uint targetID;

            NavMeshAgentComponent navMeshAgentComponent = ally.GetNavMeshAgentComponent();

            if (possibleRivals.Count == 1)
            {
                targetID = possibleRivals[0];
            }
            else
            {
                targetID = GetClosestRival<AIEnemy, AIEnemyContext, AttackComponent, AllyDamageComponent>(
                    navMeshAgentComponent.GetTransformComponent(), _aiEnemies, possibleRivals);
            }
            
            AIEnemy targetEnemy = _aiEnemies[targetID];
            
            OnAllySelectTarget(ally, targetEnemy);
        }

        private void OnAllySelectTarget(AIAlly ally, AIEnemy targetEnemy)
        {
            AIEnemyContext targetEnemyContext = targetEnemy.GetContext();

            uint groupID = targetEnemyContext.GetCurrentGroup();

            uint enemyID = targetEnemy.GetCombatAgentInstance();
            
            ally.SetRivalIndex(enemyID);
            ally.SetRivalRadius(targetEnemyContext.GetRadius());
            ally.SetHasATarget(true);
            ally.SetEnemyHealth(targetEnemyContext.GetHealth());
            ally.SetEnemyMaximumStress(targetEnemyContext.GetMaximumStress());
            ally.SetEnemyCurrentStress(targetEnemyContext.GetCurrentStress());
            ally.SetRivalGroupIDOfTarget(groupID);
            ally.SetThreatWeightOfTarget(targetEnemyContext.GetCurrentThreatGroupWeight());
            ally.SetRivalTransform(targetEnemyContext.GetAgentTransform());
        }

        private void AllyGetCloserToEnemy(AIAlly ally)
        {
            ShowActionDebugLogs(ally.name + " Getting Closer To Rival");
            //Debug.Log(ally.name + " Getting Closer To Rival");

            AIEnemy targetEnemy = _aiEnemies[ally.GetContext().GetRivalID()];
            
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
            
            ally.RequestHelp();

            AIAllyContext allyContext = ally.GetContext();

            uint currentGroup = allyContext.GetCurrentGroup();

            _moralGroups[currentGroup].Key.isRequestingHelp = true;
            _moralGroups[currentGroup].Key.SetGroupTarget(allyContext.GetRivalGroupIDOfTarget());
            
            RequestHelpToAllies(ally);
            
            EvaluateClosestPoint(ally);
            
            ShowActionDebugLogs(ally.name + " Fleeing");
            //Debug.Log(ally.name + " Fleeing");
        }

        public void RequestHelpToAllies(AIAlly ally)
        {
            foreach (AIAlly allyToSendRequest in _aiAllies.Values)
            {
                if (ally == allyToSendRequest)
                {
                    continue;
                }

                if (ally.GetContext().GetCurrentGroup() == allyToSendRequest.GetContext().GetCurrentGroup())
                {
                    continue;
                }
                
                allyToSendRequest.GetContext().SetIsAnotherMoralGroupUnderThreat(true);
                allyToSendRequest.GetContext().SetGroupHelpPriority(ally.GetContext().GetCurrentGroup(), 5);
            }
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

            Dictionary<uint, float> groupsHelpPriority = ally.GetContext().GetGroupsHelpPriority();

            uint groupIDThatNeedsMoreHelp = 0;
            float highestHelpPriority = 0;

            foreach (var groupHelpPriority in groupsHelpPriority)
            {
                float currentHelpPriority = groupHelpPriority.Value;
                if (currentHelpPriority < highestHelpPriority)
                {
                    continue;
                }

                groupIDThatNeedsMoreHelp = groupHelpPriority.Key;
                highestHelpPriority = currentHelpPriority;
            }

            foreach (AIAlly otherAlly in _aiAllies.Values)
            {
                if (otherAlly == ally)
                {
                    continue;
                }

                if (!_moralGroups[otherAlly.GetContext().GetCurrentGroup()].Key.isRequestingHelp)
                {
                    continue;
                }
            
                uint threatGroupToAttack = otherAlly.GetContext().GetRivalGroupIDOfTarget();

                int randomNumber = Random.Range(0, _enemiesIDsInsideThreatGroup[threatGroupToAttack].Count);
                
                uint enemyID = _enemiesIDsInsideThreatGroup[threatGroupToAttack][randomNumber];

                AIEnemy enemySelected = _aiEnemies[enemyID];
                
                OnAllySelectTarget(ally, enemySelected);

                TransformComponent transformComponent = enemySelected.GetNavMeshAgentComponent().GetTransformComponent();
                
                ally.SetDestination(transformComponent);
            }
            
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

            uint groupID = targetAllyContext.GetCurrentGroup();
            
            enemy.SetRivalIndex(targetAlly.GetCombatAgentInstance());
            enemy.SetRivalGroupIDOfTarget(groupID);
            enemy.SetHasATarget(true);
            enemy.SetRivalTransform(targetAllyContext.GetAgentTransform());
            
            MergeOverlappingGroupWithSameTarget<AIEnemy, AIEnemyContext, AttackComponent, AllyDamageComponent, ThreatGroupComponent>(
                enemy, groupID, _enemiesOfTheSameThreatGroupOverlappingTriggers, _aiEnemies, ref _threatGroups, MergeThreatGroups);
        }

        private void EnemyGetCloserToAlly(AIEnemy enemy)
        {
            ShowActionDebugLogs(enemy.name + " Getting Closer To Rival");
            //Debug.Log(enemy.name + " Getting Closer To Rival");

            AIAlly targetEnemy = _aiAllies[enemy.GetContext().GetRivalID()];
            
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

            uint combatInstanceID = aiAlly.GetCombatAgentInstance();
            
            foreach (uint moralGroupID in _enemiesIDsInsideThreatGroup.Keys)
            {
                if (!_alliesIDsInsideMoralGroup[moralGroupID].Contains(combatInstanceID))
                {
                    continue;
                }

                aiAlly.GetContext().SetCurrentGroup(moralGroupID);
                break;
            }
        }

        private void AddAlly(AIAlly aiAlly)
        {
            foreach (uint moralGroupID in _moralGroups.Keys)
            {
                aiAlly.AddGroupToHelp(moralGroupID);
            }
            
            AddMoralGroup(aiAlly.GetCombatAgentInstance(), aiAlly.GetMoralComponent(),
                aiAlly.GetNavMeshAgentComponent().GetTransformComponent());

            _aiAllies.Add(aiAlly.GetCombatAgentInstance(), aiAlly);
        }

        public void AddAIEnemy(AIEnemy aiEnemy)
        {
            AddEnemy(aiEnemy);

            uint combatInstanceID = aiEnemy.GetCombatAgentInstance();
            
            foreach (uint threatGroupID in _enemiesIDsInsideThreatGroup.Keys)
            {
                if (!_enemiesIDsInsideThreatGroup[threatGroupID].Contains(combatInstanceID))
                {
                    continue;
                }

                aiEnemy.GetContext().SetCurrentGroup(threatGroupID);
                break;
            }
            
            AddEnemyAttack(aiEnemy.GetAttackComponents(), GameManager.Instance.GetAllyLayer());
        }

        private void AddEnemy(AIEnemy aiEnemy)
        {
            _enemiesOfTheSameThreatGroupOverlappingTriggers.Add(aiEnemy.GetCombatAgentInstance(), new List<uint>());

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

        public void CheckEnemyThreatGroup(uint allyID, uint enemyThreatGroup)
        {
            AIAlly ally = _aiAllies[allyID];

            if (enemyThreatGroup != ally.GetContext().GetRivalGroupIDOfTarget())
            {
                return;
            }

            foreach (var moralGroup in _moralGroups)
            {
                if (moralGroup.Value.Key.GetGroupTarget() != enemyThreatGroup)
                {
                    continue;
                }
                
                OnAllyJoinsGroup(ally, moralGroup.Key);
                return;
            }
            
            OnAllyLeavesGroup(ally);
        }

        private void OnAllyJoinsGroup(AIAlly ally, uint groupToJoin)
        {
            uint allyMoralGroup = ally.GetMoralComponent().currentGroup;

            MoralGroupComponent moralGroupToJoinComponent = _moralGroups[groupToJoin].Key;

            float totalMoralWeight =
                moralGroupToJoinComponent.groupWeight + _moralGroups[allyMoralGroup].Key.groupWeight;

            bool doesThreatHasBeenOvercome = 
                _threatGroups[moralGroupToJoinComponent.GetGroupTarget()].Key.groupWeight < totalMoralWeight;

            if (doesThreatHasBeenOvercome)
            {
                _moralGroups[groupToJoin].Key.isRequestingHelp = false;

                foreach (AIAlly otherAlly in _aiAllies.Values)
                {
                    if (ally == otherAlly)
                    {
                        continue;
                    }

                    AIAllyContext allyContext = otherAlly.GetContext();

                    if (allyContext.GetCurrentGroup() == groupToJoin)
                    {
                        continue;
                    }
                    
                    allyContext.SetGroupHelpPriority(groupToJoin, 0);
                    allyContext.SetIsAnotherMoralGroupUnderThreat(false);
                }

                foreach (uint allyID in _alliesIDsInsideMoralGroup[groupToJoin])
                {
                    AIAlly groupToJoinAlly = _aiAllies[allyID];
                    FLEE_POINTS_RECORD.Remove(groupToJoinAlly);
                    groupToJoinAlly.SetDestination(new TransformComponent(groupToJoinAlly.GetContext().GetRivalTransform()));
                }
            }

            if (allyMoralGroup < groupToJoin)
            {
                List<uint> alliesIDs = new List<uint>();
                foreach (uint allyID in _alliesIDsInsideMoralGroup[groupToJoin])
                {
                    alliesIDs.Add(allyID);
                }

                if (!doesThreatHasBeenOvercome)
                {
                    _moralGroups[groupToJoin].Key.isRequestingHelp = false;
                    _moralGroups[allyMoralGroup].Key.isRequestingHelp = true;

                    foreach (AIAlly otherAlly in _aiAllies.Values)
                    {
                        if (ally == otherAlly)
                        {
                            continue;
                        }

                        AIAllyContext allyContext = otherAlly.GetContext();

                        if (allyContext.GetCurrentGroup() == groupToJoin)
                        {
                            continue;
                        }
                    
                        allyContext.SetGroupHelpPriority(groupToJoin, 0);
                        allyContext.SetIsAnotherMoralGroupUnderThreat(true);

                        if (allyContext.GetCurrentGroup() == allyMoralGroup)
                        {
                            continue;
                        }
                        
                        allyContext.SetGroupHelpPriority(allyMoralGroup, _moralGroups[groupToJoin].Key.helpPriority);
                    }
                }
                
                MergeMoralGroups(groupToJoin, allyMoralGroup);

                foreach (uint allyID in alliesIDs)
                {
                    UpdateGroupsInEnemyContext(allyID, allyMoralGroup);
                    _aiAllies[allyID].GetContext().SetCurrentGroup(allyMoralGroup);
                }
                
                return;
            }

            List<MoralComponent> moralComponents = new List<MoralComponent>
            {
                ally.GetMoralComponent()
            };
            
            MoveGivenMoralGroupToAnotherMoralGroup(moralComponents, allyMoralGroup, groupToJoin);
            UpdateGroupsInEnemyContext(ally.GetCombatAgentInstance(), groupToJoin);

            _alliesIDsInsideMoralGroup[allyMoralGroup].Remove(ally.GetCombatAgentInstance());
            _alliesIDsInsideMoralGroup[groupToJoin].Add(ally.GetCombatAgentInstance());
            ally.GetContext().SetCurrentGroup(groupToJoin);

            foreach (uint allyID in _alliesIDsInsideMoralGroup[groupToJoin])
            {
                _aiAllies[allyID].SetMoralWeight(_moralGroups[groupToJoin].Key.groupWeight);
            }
        }

        private void MergeMoralGroups(uint moralGroupFromWhichTheyCome, uint moralGroupToMove)
        {
            MoveAllMoralGroupToAnotherMoralGroup(moralGroupFromWhichTheyCome, moralGroupToMove);

            for (int i = _alliesIDsInsideMoralGroup[moralGroupFromWhichTheyCome].Count - 1; i >= 0; i--)
            {
                uint allyID = _alliesIDsInsideMoralGroup[moralGroupFromWhichTheyCome][i];
                
                _alliesIDsInsideMoralGroup[moralGroupFromWhichTheyCome].Remove(allyID);
                _alliesIDsInsideMoralGroup[moralGroupToMove].Add(allyID);
                _aiAllies[allyID].SetCurrentMoralGroup(moralGroupToMove);
            }

            foreach (uint allyID in _alliesIDsInsideMoralGroup[moralGroupToMove])
            {
                _aiAllies[allyID].GetContext().SetMoralWeight(_moralGroups[moralGroupToMove].Key.groupWeight);
            }
            
            UpdateGroupsInEnemyContext(moralGroupFromWhichTheyCome, moralGroupToMove);
        }

        private void UpdateGroupsInEnemyContext(uint allyID, uint moralGroupMoved)
        {
            foreach (AIEnemy enemy in _aiEnemies.Values)
            {
                AIEnemyContext enemyContext = enemy.GetContext();
                
                if (enemyContext.GetRivalID() != allyID)
                {
                    continue;
                }
                
                enemyContext.SetRivalGroupIDOfTarget(moralGroupMoved);
            }
        }

        private void OnAllyLeavesGroup(AIAlly aiAlly)
        {
            MoralComponent moralComponent = aiAlly.GetMoralComponent();
            uint originalMoralGroup = moralComponent.GetOriginalGroup();
            uint currentMoralGroup = aiAlly.GetContext().GetCurrentGroup();
            
            uint allyCombatAgentInstance = aiAlly.GetCombatAgentInstance();
            
            List<MoralComponent> moralComponents = new List<MoralComponent>();

            if (originalMoralGroup != currentMoralGroup)
            {
                moralComponents.Add(moralComponent);
                MoveGivenMoralGroupToAnotherMoralGroup(moralComponents, currentMoralGroup, originalMoralGroup);
                _alliesIDsInsideMoralGroup[currentMoralGroup].Remove(allyCombatAgentInstance);
                _alliesIDsInsideMoralGroup[originalMoralGroup].Add(allyCombatAgentInstance);
                aiAlly.SetCurrentMoralGroup(originalMoralGroup);
                
                UpdateGroupsInEnemyContext(allyCombatAgentInstance, originalMoralGroup);
                
                return;
            }

            List<uint> alliesIDs = new List<uint>();

            foreach (uint allyID in _alliesIDsInsideMoralGroup[currentMoralGroup])
            {
                if (allyID == allyCombatAgentInstance)
                {
                    continue;
                }
                moralComponents.Add(_aiAllies[allyID].GetMoralComponent());
                alliesIDs.Add(allyID);
            }

            if (moralComponents.Count == 0)
            {
                return;
            }

            uint lowestMoralGroup = FindLowestGroupID(moralComponents);
            
            MoveGivenMoralGroupToAnotherMoralGroup(moralComponents, currentMoralGroup, lowestMoralGroup);

            foreach (uint allyID in alliesIDs)
            {
                _alliesIDsInsideMoralGroup[currentMoralGroup].Remove(allyID);
                _alliesIDsInsideMoralGroup[lowestMoralGroup].Add(allyID);
                _aiAllies[allyID].SetCurrentMoralGroup(lowestMoralGroup);
                
                UpdateGroupsInEnemyContext(allyID, lowestMoralGroup);
            }
        }

        public void OnEnemyJoinEnemy(AIEnemy enemy, uint otherEnemyID)
        {
            AIEnemy otherEnemy = _aiEnemies[otherEnemyID];

            if (enemy.GetContext().GetRivalID() != otherEnemy.GetContext().GetRivalID())
            {
                return;
            }
            
            _enemiesOfTheSameThreatGroupOverlappingTriggers[enemy.GetCombatAgentInstance()].Add(otherEnemyID);

            uint enemyThreatGroup = enemy.GetThreatComponent().currentGroup;
            uint otherEnemyThreatGroup = otherEnemy.GetThreatComponent().currentGroup;

            if (enemyThreatGroup < otherEnemyThreatGroup)
            {
                return;
            }

            MergeThreatGroups(enemyThreatGroup, otherEnemyThreatGroup);
        }

        private void MergeOverlappingGroupWithSameTarget<TAgent, TContext, TAttackComponent, TDamageComponent, TGroupComponent>(TAgent agent, 
            uint groupID, Dictionary<uint, List<uint>> agentsOfTheSameGroupOverlappingTriggers, 
            Dictionary<uint, TAgent> aiFactions, ref Dictionary<uint, KeyValuePair<TGroupComponent, VectorComponent>> factionGroups, 
            Action<uint, uint> mergeGroupsAction)
        
            where TAgent : AICombatAgentEntity<TContext, TAttackComponent, TDamageComponent>
            where TContext : AICombatAgentContext
            where TAttackComponent : AttackComponent
            where TDamageComponent : DamageComponent
            where TGroupComponent : GroupComponent
        {
            List<uint> overlappingThreatGroupsWithSameTarget =
                GetOverlappingGroupsWithSameTarget<TAgent, TContext, TAttackComponent, TDamageComponent>(groupID,
                    agentsOfTheSameGroupOverlappingTriggers[agent.GetCombatAgentInstance()], aiFactions);

            uint agentOriginalThreatGroup = agent.GetGroupComponent().GetOriginalGroup();
            
            if (overlappingThreatGroupsWithSameTarget.Count == 0)
            {
                factionGroups[agentOriginalThreatGroup].Key.SetGroupTarget(groupID);
                return;
            }
            
            overlappingThreatGroupsWithSameTarget.Add(agentOriginalThreatGroup);
            
            overlappingThreatGroupsWithSameTarget.Sort();

            factionGroups[overlappingThreatGroupsWithSameTarget[0]].Key.SetGroupTarget(groupID);

            for (int i = overlappingThreatGroupsWithSameTarget.Count - 1; i >= 1; i--)
            {
                mergeGroupsAction(overlappingThreatGroupsWithSameTarget[i],
                    overlappingThreatGroupsWithSameTarget[i - 1]);
            }
        }

        private List<uint> GetOverlappingGroupsWithSameTarget<TAgent, TContext, TAttackComponent, TDamageComponent>(
            uint targetID, List<uint> alliesIDs, Dictionary<uint, TAgent> aiFactions)
        
            where TAgent : AICombatAgentEntity<TContext, TAttackComponent, TDamageComponent>
            where TContext : AICombatAgentContext
            where TAttackComponent : AttackComponent
            where TDamageComponent : DamageComponent
        {
            List<uint> moralGroups = new List<uint>();
            List<uint> moralGroupsChecked = new List<uint>();

            foreach (uint allyID in alliesIDs)
            {
                TAgent ally = aiFactions[allyID];

                uint currentMoralGroup = ally.GetContext().GetCurrentGroup();

                if (moralGroupsChecked.Contains(currentMoralGroup))
                {
                    continue;
                }
                
                moralGroupsChecked.Add(currentMoralGroup);
                
                if (ally.GetContext().GetRivalID() == targetID)
                {
                    continue;
                }
                
                moralGroups.Add(currentMoralGroup);
            }

            return moralGroups;
        }

        private void MergeThreatGroups(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            MoveAllThreatGroupToAnotherThreatGroup(threatGroupFromWhichTheyCome, threatGroupToMove);

            for (int i = _enemiesIDsInsideThreatGroup[threatGroupFromWhichTheyCome].Count - 1; i >= 0; i--)
            {
                uint enemyID = _enemiesIDsInsideThreatGroup[threatGroupFromWhichTheyCome][i];
                
                _enemiesIDsInsideThreatGroup[threatGroupFromWhichTheyCome].Remove(enemyID);
                _enemiesIDsInsideThreatGroup[threatGroupToMove].Add(enemyID);
                _aiEnemies[enemyID].SetCurrentThreatGroup(threatGroupToMove);
            }
            
            UpdateThreatInAllyContext(threatGroupFromWhichTheyCome, threatGroupToMove);
        }

        public void OnEnemySeparateFromEnemy(AIEnemy aiEnemy, uint otherEnemyID)
        {
            AIEnemy otherEnemy = _aiEnemies[otherEnemyID];
            
            _enemiesOfTheSameThreatGroupOverlappingTriggers[aiEnemy.GetCombatAgentInstance()].Remove(otherEnemyID);

            List<AIEnemy> allContacts = GetAllContacts(aiEnemy);

            List<ThreatComponent> threatComponents = new List<ThreatComponent>();

            foreach (AIEnemy aiEnemyInContact in allContacts)
            {
                threatComponents.Add(aiEnemyInContact.GetThreatComponent());
            }

            uint lowestThreatGroup = FindLowestGroupID(threatComponents);
            uint otherThreatGroup = otherEnemy.GetThreatComponent().currentGroup;

            if (lowestThreatGroup == otherThreatGroup)
            {
                return;
            }

            MoveGivenThreatsToAnotherThreatGroup(threatComponents, otherThreatGroup, lowestThreatGroup);

            foreach (AIEnemy enemy in allContacts)
            {
                uint combatAgentInstance = enemy.GetCombatAgentInstance();
                _enemiesIDsInsideThreatGroup[otherThreatGroup].Remove(combatAgentInstance);
                _enemiesIDsInsideThreatGroup[lowestThreatGroup].Add(combatAgentInstance);
                enemy.SetCurrentThreatGroup(lowestThreatGroup);
            }

            UpdateThreatInAllyContext(otherThreatGroup, lowestThreatGroup);
        }

        private void UpdateThreatInAllyContext(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
        {
            foreach (AIAlly ally in _aiAllies.Values)
            {
                uint rivalGroupIDOfTarget = ally.GetContext().GetRivalGroupIDOfTarget();
                if (rivalGroupIDOfTarget != threatGroupFromWhichTheyCome && 
                    rivalGroupIDOfTarget != threatGroupToMove)
                {
                    continue;
                }

                ally.SetRivalGroupIDOfTarget(threatGroupToMove);
                ally.SetThreatWeightOfTarget(_threatGroups[threatGroupToMove].Key.groupWeight);
            }
        }

        private List<AIEnemy> GetAllContacts(AIEnemy aiAgent)
        {
            List<AIEnemy> contacts = new List<AIEnemy>();

            Stack<AIEnemy> aiAgentsStack = new Stack<AIEnemy>();
            aiAgentsStack.Push(aiAgent);

            while (aiAgentsStack.Count > 0)
            {
                AIEnemy currentAgent = aiAgentsStack.Pop();

                contacts.Add(currentAgent);

                foreach (uint aiAgentIDToCheck in _enemiesOfTheSameThreatGroupOverlappingTriggers[currentAgent.GetCombatAgentInstance()])
                {
                    AIEnemy aiAgentToCheck = _aiEnemies[aiAgentIDToCheck];
                    
                    if (!contacts.Contains(aiAgentToCheck))
                    {
                        aiAgentsStack.Push(aiAgentToCheck);
                    }
                }
            }

            return contacts;
        }

        private uint FindLowestGroupID<TStatComponent>(List<TStatComponent> statComponents)
            where TStatComponent : IGroup
        {
            uint lowestGroupID = statComponents[0].GetOriginalGroup();

            foreach (TStatComponent statComponent in statComponents)
            {
                uint currentOriginalGroupIndex = statComponent.GetOriginalGroup();

                if (lowestGroupID < currentOriginalGroupIndex)
                {
                    continue;
                }

                lowestGroupID = currentOriginalGroupIndex;
            }

            return lowestGroupID;
        }

        private List<uint> GetPossibleRivals(List<uint> visibleRivals, List<uint> threatGroupsToAvoid, float moralWeight)
        {
            List<uint> auxVisibleRivals = new List<uint>();
            
            auxVisibleRivals.AddRange(visibleRivals);
            
            for (int i = visibleRivals.Count - 1; i >= 0; i--)
            {
                uint enemyID = visibleRivals[i];

                AIEnemyContext enemyContext = _aiEnemies[enemyID].GetContext();

                if (!threatGroupsToAvoid.Contains(enemyContext.GetCurrentGroup()))
                {
                    continue;
                }
                
                visibleRivals.RemoveAt(i);
            }

            return visibleRivals.Count == 0 ? auxVisibleRivals : visibleRivals;
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
                if (ally.GetContext().GetRivalID() != enemyAgentInstanceID)
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
                if (ally.GetContext().GetRivalID() != enemyAgentInstanceID)
                {
                    continue;
                }
                
                ally.SetIsEnemyStunned(false);
            }
        }

        public void OnEnemyDefeated(AIEnemy aiEnemy)
        {
            uint combatAgentInstance = aiEnemy.GetCombatAgentInstance();

            ThreatComponent threatComponent = aiEnemy.GetThreatComponent();
            
            EraseThreat(combatAgentInstance, threatComponent, aiEnemy.GetNavMeshAgentComponent().GetTransformComponent());

            List<uint> enemiesOverlapping = _enemiesOfTheSameThreatGroupOverlappingTriggers[aiEnemy.GetCombatAgentInstance()];

            for (int i = enemiesOverlapping.Count - 1; i >= 0; i--)
            {
                OnEnemySeparateFromEnemy(_aiEnemies[enemiesOverlapping[i]], aiEnemy.GetCombatAgentInstance());
            }

            _aiEnemies.Remove(combatAgentInstance);
            _enemiesIDsInsideThreatGroup.Remove(threatComponent.GetOriginalGroup());
            _groupThreatsComponents.Remove(threatComponent.GetOriginalGroup());
            _threatGroups.Remove(threatComponent.GetOriginalGroup());
            
            
            
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
                if (agent.GetContext().GetRivalID() != aiCombatAgentDefeated.GetCombatAgentInstance())
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
                enemy.NotAttacking();
                return;
            }
            
            AIAttackCollider attackCollider = _enemiesAttacksColliders[attackComponent];
            attackCollider.SetParent(attackerTransform);
            attackCollider.gameObject.SetActive(true);
            StartCoroutine(StartEnemyAttackCastTimeCoroutine(attackComponent, attackCollider, enemy));
        }

        private void PutAttackOnCooldown(AttackComponent attackComponent, AIEnemy enemy)
        {
            enemy.NotAttacking();
            StartCoroutine(StartCooldownCoroutine(attackComponent, enemy));
        }

        #endregion

        #region Systems

        #region Moral System

        private void AddMoralGroup(uint combatAgentID, MoralComponent moralComponent,
            TransformComponent transformComponent)
        {
            uint moralGroupID = moralComponent.GetOriginalGroup();

            List<uint> alliesIDs = new List<uint>
            {
                combatAgentID
            };

            List<MoralComponent> moralComponents = new List<MoralComponent>
            {
                moralComponent
            };

            List<TransformComponent> transformComponents = new List<TransformComponent>
            {
                transformComponent
            };
            
            _alliesIDsInsideMoralGroup.Add(moralGroupID, alliesIDs);

            foreach (AIAlly ally in _aiAllies.Values)
            {
                ally.AddGroupToHelp(moralGroupID);
            }

            _groupMoralsComponents.Add(moralGroupID,
                new KeyValuePair<List<MoralComponent>, List<TransformComponent>>(moralComponents, transformComponents));

            MoralGroupComponent moralGroupComponent = new MoralGroupComponent(0);
            VectorComponent vectorComponent = new VectorComponent(new Vector3());
            
            _moralGroups.Add(moralGroupID, 
                new KeyValuePair<MoralGroupComponent, VectorComponent>(moralGroupComponent, vectorComponent));

            _moralGroups[moralGroupID].Key.groupWeight = moralComponent.GetWeight();
        }

        private void EraseMoralGroup(uint combatAgentID, MoralComponent moralComponent, TransformComponent transformComponent)
        {
            uint originalGroup = moralComponent.GetOriginalGroup();
            uint currentGroup = moralComponent.GetCurrentGroup();

            _alliesIDsInsideMoralGroup[currentGroup].Remove(combatAgentID);
            _moralGroups[currentGroup].Key.groupWeight -= moralComponent.GetWeight();
            _groupMoralsComponents[currentGroup].Key.Remove(moralComponent);
            _groupMoralsComponents[currentGroup].Value.Remove(transformComponent);

            _groupMoralsComponents.Remove(originalGroup);
        }

        private void MoveAllMoralGroupToAnotherMoralGroup(uint moralGroupFromWhichTheyCome, uint moralGroupToMove)
        {
            for (int i = 0; i < _groupMoralsComponents[moralGroupFromWhichTheyCome].Key.Count; i++)
            {
                MoveSingleMoralComponentToAnotherMoralGroup(_groupMoralsComponents[moralGroupFromWhichTheyCome].Key[i], 
                    moralGroupFromWhichTheyCome, moralGroupToMove);
            }
        }

        private void MoveGivenMoralGroupToAnotherMoralGroup(List<MoralComponent> moralComponentsToMove, 
            uint moralGroupFromWhichTheyCome, uint moralGroupToMove)
        {
            foreach (MoralComponent moralComponent in moralComponentsToMove)
            {
                MoveSingleMoralComponentToAnotherMoralGroup(moralComponent, moralGroupFromWhichTheyCome, moralGroupToMove);
            }
        }

        private void MoveSingleMoralComponentToAnotherMoralGroup(MoralComponent moralComponent, uint moralGroupFromWhichComes, 
            uint moralGroupToMove)
        {
            int moralComponentListIndex = _groupMoralsComponents[moralGroupFromWhichComes].Key.IndexOf(moralComponent);

            if (moralComponentListIndex == -1)
            {
                return;
            }
            
            MoveMoralComponentToMoralGroup(_groupMoralsComponents[moralGroupFromWhichComes].Key[moralComponentListIndex],
                moralGroupFromWhichComes, moralGroupToMove);
                    
            MoveTransformComponentToMoralGroup(_groupMoralsComponents[moralGroupFromWhichComes].Value[moralComponentListIndex],
                moralGroupFromWhichComes, moralGroupToMove);
        }

        private void MoveGroupComponentToGroup<TStatComponent>(TStatComponent statComponent, uint groupFromWhichTheyCome,
            uint groupToMove, ref Dictionary<uint, KeyValuePair<List<TStatComponent>, List<TransformComponent>>> groupStatComponents,
            Dictionary<uint, KeyValuePair<GroupComponent, VectorComponent>> factionGroups)
        
            where TStatComponent : IStatWeight
        {
            groupStatComponents[groupFromWhichTheyCome].Key.Remove(statComponent);
            factionGroups[groupFromWhichTheyCome].Key.groupWeight -= statComponent.GetWeight();
            
            
        }

        private void MoveMoralComponentToMoralGroup(MoralComponent moralComponent, uint moralGroupFromWhichTheyCome, 
            uint moralGroupToMove)
        {
            _groupMoralsComponents[moralGroupFromWhichTheyCome].Key.Remove(moralComponent);
            _moralGroups[moralGroupFromWhichTheyCome].Key.groupWeight -= moralComponent.GetWeight();
            
            moralComponent.currentGroup = moralGroupToMove;
            _groupMoralsComponents[moralGroupToMove].Key.Add(moralComponent);
            _moralGroups[moralGroupToMove].Key.groupWeight += moralComponent.GetWeight();
        }

        private void MoveTransformComponentToMoralGroup(TransformComponent transformComponent, 
            uint moralGroupFromWhichTheyCome, uint moralGroupToMove)
        {
            _groupMoralsComponents[moralGroupFromWhichTheyCome].Value.Remove(transformComponent);
            
            _groupMoralsComponents[moralGroupToMove].Value.Add(transformComponent);
        }

        #endregion

        #region Threat System

        private void UpdateThreatGroupsBarycenter()
        {
            for (uint i = 1; i < _groupThreatsComponents.Count + 1; i++)
            {
                if (!_groupThreatsComponents.ContainsKey(i))
                {
                    continue;
                }
                
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
            foreach (KeyValuePair<uint, List<uint>> enemiesInsideThreatGroup in _enemiesIDsInsideThreatGroup)
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

        private void AddThreat(uint combatAgentID, ThreatComponent threatComponent, TransformComponent transformComponent)
        {
            uint threatGroupID = threatComponent.GetOriginalGroup();

            List<uint> enemiesIDs = new List<uint>
            {
                combatAgentID
            };
            
            List<ThreatComponent> threatComponents = new List<ThreatComponent>
            {
                threatComponent
            };
            
            List<TransformComponent> transformComponents = new List<TransformComponent>
            {
                transformComponent
            };
            
            _enemiesIDsInsideThreatGroup.Add(threatGroupID, enemiesIDs);
            
            _groupThreatsComponents.Add(threatGroupID, 
                new KeyValuePair<List<ThreatComponent>, List<TransformComponent>>(threatComponents, transformComponents));

            ThreatGroupComponent threatGroupComponent = new ThreatGroupComponent(0);
            VectorComponent vectorComponent = new VectorComponent(new Vector3());
            
            _threatGroups.Add(threatGroupID, 
                new KeyValuePair<ThreatGroupComponent, VectorComponent>(threatGroupComponent, vectorComponent));
            
            _threatGroups[threatGroupID].Key.groupWeight = threatComponent.GetWeight();
        }

        private void EraseThreat(uint combatAgentID, ThreatComponent threatComponent, TransformComponent transformComponent)
        {
            uint currentGroup = threatComponent.GetCurrentGroup();

            _enemiesIDsInsideThreatGroup[currentGroup].Remove(combatAgentID);
            _threatGroups[currentGroup].Key.groupWeight -= threatComponent.GetWeight();
            _groupThreatsComponents[currentGroup].Key.Remove(threatComponent);
            _groupThreatsComponents[currentGroup].Value.Remove(transformComponent);
        }

        private void MoveAllThreatGroupToAnotherThreatGroup(uint threatGroupFromWhichTheyCome, uint threatGroupToMove)
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
            _threatGroups[threatGroupFromWhichTheyCome].Key.groupWeight -= threatComponent.GetWeight();
            
            threatComponent.currentGroup = threatGroupToMove;
            _groupThreatsComponents[threatGroupToMove].Key.Add(threatComponent);
            _threatGroups[threatGroupToMove].Key.groupWeight += threatComponent.GetWeight();
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
                closestPoint = closestPointOnEdge + (new Vector2(direction.y, -direction.x).normalized * (radius * 1.5f));
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