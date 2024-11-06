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
using UnityEngine;

namespace Managers
{
    public class CombatManager : MonoBehaviour
    {
        private static CombatManager _instance;

        public static CombatManager Instance => _instance;

        private Dictionary<uint, AICombatAgentEntity<AICombatAgentContext>> _aiCombatAgentInstanceIDs =
            new Dictionary<uint, AICombatAgentEntity<AICombatAgentContext>>();

        private Dictionary<AIAgentType, Delegate> _returnTheSameAgentsType = new Dictionary<AIAgentType, Delegate>
        {
            { AIAgentType.ALLY, new Func<List<AIAlly>>(() => 
                _instance.ReturnAllDictionaryValuesInAList<AIAlly ,AIAllyContext>(_instance._aiAllies)) },
            
            { AIAgentType.ENEMY, new Func<List<AIEnemy>>(() => 
                _instance.ReturnAllDictionaryValuesInAList<AIEnemy, AIEnemyContext>(_instance._aiEnemies)) }
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
            { AIEnemyAction.ATTACK , enemy => Instance.EnemyAttack(enemy)},
            { AIEnemyAction.FLEE , enemy => Instance.EnemyFlee(enemy)}
        };

        private Dictionary<AttackComponent, AIAttackCollider> _attacksColliders =
            new Dictionary<AttackComponent, AIAttackCollider>();

        private Dictionary<AIEnemy, List<uint>> _enemiesOfTheSameThreatGroupOverlappingTriggers =
            new Dictionary<AIEnemy, List<uint>>();

        private Dictionary<uint, List<uint>> _enemiesIndexesInsideThreatGroup = new Dictionary<uint, List<uint>>();

        private Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>> _groupThreatsComponents =
                new Dictionary<uint, KeyValuePair<List<ThreatComponent>, List<TransformComponent>>>();

        private Dictionary<uint, KeyValuePair<ThreatGroupComponent, VectorComponent>> _threatGroups =
                new Dictionary<uint, KeyValuePair<ThreatGroupComponent, VectorComponent>>();

        private AIAllyUtilityFunction _allyUtilityFunction = new AIAllyUtilityFunction();
        private AIEnemyUtilityFunction _enemyUtilityFunction = new AIEnemyUtilityFunction();
        
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
                TERRAIN_POSITIONS = GetTerrainPositions(FLEE_POINTS);
                //

                return;
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            UpdateThreatGroupsBarycenter();

            UpdateThreatGroupsRadius();

            /*_combatSystem.UpdateCombatState(_aiCombatAgentsTargets);

            _fleeSystem.UpdateFleeMovement(ref FLEE_POINTS_RECORD, TERRAIN_POSITIONS);*/
        }

        #region UBS
        
        public List<uint> GetVisibleRivals<TAgent, TRivalContext, TOwnContext>(AICombatAgentEntity<TOwnContext> aiCombatAgent)
        where TAgent : AICombatAgentEntity<TRivalContext>
        where TRivalContext : AICombatAgentContext
        where TOwnContext : AICombatAgentContext
        {
            AIAgentType ownAgentType = aiCombatAgent.GetAIAgentType();

            List<uint> visibleRivals = new List<uint>();

            List<AICombatAgentEntity<TRivalContext>> rivals = ReturnAllRivals<TAgent, TRivalContext>(ownAgentType);

            float sightMaximumDistance = aiCombatAgent.GetContext().GetSightMaximumDistance();

            foreach (AICombatAgentEntity<TRivalContext> rival in rivals)
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
            
            //CheckIfIsAlreadyPerformingThisAction<AIAlly, AIAllyContext, AIAllyAction>(ally, allyAction, AllyPerformAction);
            
            AllyPerformAction(ally, allyAction);
        }

        public void CalculateBestAction(AIEnemy enemy)
        {
            AIEnemyAction enemyAction = CalculateBestAction<AIEnemyAction, AIEnemyContext>(enemy.GetContext(), _enemyUtilityFunction);
            
            //CheckIfIsAlreadyPerformingThisAction<AIEnemy, AIEnemyContext, AIEnemyAction>(enemy, enemyAction, EnemyPerformAction);
            
            EnemyPerformAction(enemy, enemyAction);
        }

        private static TAction CalculateBestAction<TAction, TContext>(TContext context, 
            IGetBestAction<TAction, TContext> utilityCalculator)
        {
            return utilityCalculator.GetBestAction(context);
        }

        private static void CheckIfIsAlreadyPerformingThisAction<TAgent, TContext, TAction>(TAgent agent, TAction agentAction, 
            Action<TAgent, TAction> action)
        where TAgent : AICombatAgentEntity<TContext> 
        where TContext : AICombatAgentContext
        where TAction : Enum
        {
            uint agentActionUInt = Convert.ToUInt16(agentAction);
            
            if (agent.GetContext().GetLastActionIndex() == agentActionUInt)
            {
                return;
            }
            
            agent.SetLastActionIndex(agentActionUInt);

            action(agent, agentAction);
        }

        #region Ally

        public List<uint> FilterThreatGroupsThatThreatMe(uint combatAgentInstance, IStatWeight moralWeightComponent,
            float threatWeightOfCurrentTarget, List<uint> visibleRivalsIDs)
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

                if (threatGroupComponent.threatGroupWeight + threatWeightOfCurrentTarget < moralWeightComponent.GetWeight())
                {
                    continue;
                }
                
                threatGroupsThatThreatMe.Add(currentThreatGroupID);
            }

            return threatGroupsThatThreatMe;
        }

        public uint[] FilterPerThreatGroupAlliesFighting(AICombatAgentEntity<AIAllyContext> combatAgent)
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
            //TODO
            Debug.Log(ally.name + " Following Player");
        }

        private void AllyRequestRival(AIAlly ally)
        {
            List<uint> visibleRivals = ally.GetVisibleRivals();
            
            Debug.Log(ally.name + " Requesting Rival");
            
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
                targetId = GetClosestRival(navMeshAgentComponent.GetTransformComponent(), visibleRivals);
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
            Debug.Log(ally.name + " Getting Closer To Rival");

            AIEnemy targetEnemy = _aiEnemies[ally.GetContext().GetRivalIndex()];
            
            ECSNavigationManager.Instance.UpdateNavMeshAgentTransformDestination(ally.GetNavMeshAgentComponent(),
                targetEnemy.GetNavMeshAgentComponent().GetTransformComponent());
        }

        private void AllyAttack(AIAlly ally)
        {
            Debug.Log(ally.name + " Attacking");
                
            ally.SetIsAttacking(true);
            
            StartCastingAnAttack(ally.transform, ally.GetAttackComponents()[0]);
        }

        private void AllyFlee(AIAlly ally)
        {
            //TODO
            Debug.Log(ally.name + " Fleeing");
        }

        private void AllyDodge(AIAlly ally)
        {
            //TODO
            Debug.Log(ally.name + " Dodging");
        }

        private void AllyHelpAnotherAlly(AIAlly ally)
        {
            //TODO
            Debug.Log(ally.name + " Helping another ally");
        }

        #endregion
        
        #region Enemy

        private void EnemyPerformAction(AIEnemy enemy, AIEnemyAction enemyAction)
        {
            _aiEnemyActions[enemyAction](enemy);
        }

        private void EnemyPatrol(AIEnemy enemy)
        {
            //TODO
            Debug.Log(enemy.name + " Patrolling");
        }

        private void EnemyRequestRival(AIEnemy enemy)
        {
            List<uint> visibleRivals = enemy.GetVisibleRivals();
            
            Debug.Log(enemy.name + " Requesting Rival");

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
                targetId = GetClosestRival(navMeshAgentComponent.GetTransformComponent(), visibleRivals);
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
            Debug.Log(enemy.name + " Getting Closer To Rival");

            AIAlly targetEnemy = _aiAllies[enemy.GetContext().GetRivalIndex()];
            
            ECSNavigationManager.Instance.UpdateNavMeshAgentTransformDestination(enemy.GetNavMeshAgentComponent(),
                targetEnemy.GetNavMeshAgentComponent().GetTransformComponent());
        }

        private void EnemyAttack(AIEnemy enemy)
        {
            Debug.Log(enemy.name + " Attacking");
            
            enemy.SetIsAttacking(true);
            
            StartCastingAnAttack(enemy.transform, enemy.GetAttackComponents()[0]);
        }

        private void EnemyFlee(AIEnemy enemy)
        {
            //TODO
            Debug.Log(enemy.name + " Fleeing");
        }

        #endregion

        #endregion

        #region Add Combat Agent

        public void AddAIAlly(AIAlly aiAlly, AIAllyContext aiAllyContext)
        {
            AddAlly(aiAlly);
            AddAttack(aiAlly.GetAttackComponents(), GameManager.Instance.GetEnemyLayer());
            ECSNavigationManager.Instance.AddNavMeshAgentEntity(aiAlly.GetNavMeshAgentComponent());
        }

        private void AddAlly(AIAlly aiAlly)
        {
            //TODO moral system

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
            
            AddAttack(aiEnemy.GetAttackComponents(), GameManager.Instance.GetAllyLayer());
            ECSNavigationManager.Instance.AddNavMeshAgentEntity(aiEnemy.GetNavMeshAgentComponent());
        }

        private void AddEnemy(AIEnemy aiEnemy)
        {
            _enemiesOfTheSameThreatGroupOverlappingTriggers.Add(aiEnemy, new List<uint>());

            AddThreat(aiEnemy.GetCombatAgentInstance(), aiEnemy.GetThreatComponent(), aiEnemy.GetNavMeshAgentComponent().GetTransformComponent());
            
            _aiEnemies.Add(aiEnemy.GetCombatAgentInstance(), aiEnemy);
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

            foreach (uint enemyIndex in _enemiesIndexesInsideThreatGroup[threatGroupFromWhichTheyCome])
            {
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

        private uint GetClosestRival(IPosition positionComponent,
            List<uint> possibleTargetsAICombatAgentIDs)
        {
            uint targetID = 0;
            AICombatAgentEntity<AICombatAgentContext> currentTarget;

            float targetDistance = 300000000;
            float currentTargetDistance;

            for (int i = 0; i < possibleTargetsAICombatAgentIDs.Count; i++)
            {
                currentTarget = _aiCombatAgentInstanceIDs[possibleTargetsAICombatAgentIDs[i]];

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
        
        private void OnAllyDefeated(AIAlly aiAlly)
        {
            //TODO
            
            OnAgentDefeated<AIEnemy, AIEnemyContext, AIAllyContext>(aiAlly, ref _aiEnemies);
        }

        public void OnEnemyReceiveDamage(uint enemyAgentInstanceID, uint enemyHealth, float enemyStress)
        {
            foreach (AIAlly ally in _aiAllies.Values)
            {
                if (ally.GetContext().GetRivalIndex() != enemyAgentInstanceID)
                {
                    continue;
                }

                ally.SetEnemyHealth(enemyHealth);
                ally.SetEnemyCurrentStress(enemyStress);
            }
        }

        public void OnEnemyDefeated(AIEnemy aiEnemy)
        {
            uint combatAgentInstance = aiEnemy.GetCombatAgentInstance();

            _aiEnemies.Remove(combatAgentInstance);
            
            EraseThreat(combatAgentInstance, aiEnemy.GetThreatComponent(), aiEnemy.GetNavMeshAgentComponent().GetTransformComponent());

            List<uint> enemiesOverlapping = _enemiesOfTheSameThreatGroupOverlappingTriggers[aiEnemy];

            for (int i = enemiesOverlapping.Count - 1; i >= 0; i--)
            {
                OnEnemySeparateFromEnemy(_aiEnemies[enemiesOverlapping[i]], aiEnemy.GetCombatAgentInstance());
            }
            
            OnAgentDefeated<AIAlly, AIAllyContext, AIEnemyContext>(aiEnemy, ref _aiAllies);
        }

        private void OnAgentDefeated<TAgent, TRivalContext, TOwnContext>(
            AICombatAgentEntity<TOwnContext> aiCombatAgentDefeated, ref Dictionary<uint, TAgent> agents)
        where TAgent : AICombatAgentEntity<TRivalContext> 
        where TRivalContext : AICombatAgentContext
        where TOwnContext : AICombatAgentContext
        {
            foreach (TAgent agent in agents.Values)
            {
                if (agent.GetContext().GetRivalIndex() != aiCombatAgentDefeated.GetCombatAgentInstance())
                {
                    continue;
                }
                
                //RequestRival(attackerAndTarget.Key);
            }
        }

        public void OnAllyDefeated(uint allyAgentInstanceID)
        {
            
        }

        public void OnEnemyDefeated(uint enemyAgentInstanceID)
        {
            
        }

        #endregion

        #region Flee Events

        public void RequestSafeSpot(AICombatAgentEntity<AIAllyContext> aiCombatAgentEntity)
        {
            
        }

        #endregion

        #region Attack Events

        private void StartCastingAnAttack(Transform attackerTransform, AttackComponent attackComponent)
        {
            if (attackComponent.IsOnCooldown())
            {
                return;
            }
            
            AIAttackCollider attackCollider = _attacksColliders[attackComponent];
            attackCollider.SetParent(attackerTransform);
            attackCollider.gameObject.SetActive(true);
            StartCoroutine(StartCastTimeCoroutine(attackComponent, attackCollider));
        }

        public void PutAttackOnCooldown(AttackComponent attackComponent)
        {
            StartCoroutine(StartCooldownCoroutine(attackComponent));
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
                uint threatGroupIndex = enemiesInsideThreatGroup.Key;

                Vector3 threatGroupBarycenter = _threatGroups[threatGroupIndex].Value.GetPosition();

                List<uint> enemiesIDs = enemiesInsideThreatGroup.Value;

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
                    farthestEnemyDistanceToBarycenter + farthestEnemyFromTheBarycenter.GetContext().GetRadius();
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

        private void UpdateDistanceToRival<TContext>(AICombatAgentEntity<TContext> combatAgent)
        where TContext : AICombatAgentContext
        {
            Vector3 vectorToRival = combatAgent.GetContext().GetRivalTransform().position - combatAgent.transform.position;
            
            combatAgent.SetVectorToRival(vectorToRival);
            combatAgent.SetDistanceToRival(vectorToRival.magnitude);
        }

        #endregion
        
        #region Attack System

        private IEnumerator StartCastTimeCoroutine(AttackComponent attackComponent, AIAttackCollider attackCollider)
        {
            Instance.PutAttackOnCooldown(attackComponent);
            attackComponent.StartCastTime();
            while (attackComponent.IsCasting())
            {
                attackComponent.DecreaseCurrentCastTime();
                yield return null;
            }
            
            attackCollider.Deactivate();
        }

        private IEnumerator StartCooldownCoroutine(AttackComponent attackComponent)
        {
            attackComponent.StartCooldown();
            while (attackComponent.IsOnCooldown())
            {
                attackComponent.DecreaseCooldown();
                yield return null;
            }
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
        
        private void EvaluateClosesPoint<TContext>(ref Dictionary<AICombatAgentEntity<TContext>, int> FLEE_POINTS_RECORD, 
            List<Vector3> FLEE_POINTS, AICombatAgentEntity<TContext> combatAgentNeedsToFlee)
        where TContext : AICombatAgentContext
        {
            float closestDistance = 300000;
            
            Vector3 agentPosition = combatAgentNeedsToFlee.transform.position;
            Vector3 destination = new Vector3();

            foreach (Vector3 position in FLEE_POINTS)
            {
                float currentDistance = (agentPosition- position).magnitude;
                if (currentDistance > closestDistance)
                {
                    continue;
                }

                closestDistance = currentDistance;
                destination = position;
            }
            
            ECSNavigationManager.Instance.UpdateNavMeshAgentVectorDestination(combatAgentNeedsToFlee.GetNavMeshAgentComponent(), 
                new VectorComponent(destination));
        }

        private void UpdateFleeMovement(ref Dictionary<AIAlly, int> FLEE_POINTS_RECORD, 
            List<Vector3> FLEE_POINTS)
        {
            foreach (var combatAgentFleeing in FLEE_POINTS_RECORD)
            {
                AIAlly combatAgent = combatAgentFleeing.Key;
                int index = combatAgentFleeing.Value;

                if ((combatAgent.transform.position - FLEE_POINTS[index]).magnitude < 8)
                {
                    int newIndex = (combatAgentFleeing.Value + 1) % FLEE_POINTS.Count;

                    FLEE_POINTS_RECORD[combatAgent] = newIndex;
                    
                    ECSNavigationManager.Instance.UpdateNavMeshAgentVectorDestination(combatAgent.GetNavMeshAgentComponent(), 
                        new VectorComponent(FLEE_POINTS[newIndex]));
                }
            }
        }
        //

        #endregion
        
        #endregion

        private List<TCombatAgent> ReturnAllDictionaryValuesInAList<TCombatAgent, TContext>(Dictionary<uint, TCombatAgent> agentsDictionary)
        where TCombatAgent : AICombatAgentEntity<TContext> where TContext : AICombatAgentContext
        {
            List<TCombatAgent> agentsList = new List<TCombatAgent>();

            foreach (TCombatAgent combatAgent in agentsDictionary.Values)
            {
                agentsList.Add(combatAgent);
            }

            return agentsList;
        }

        private List<AICombatAgentEntity<TContext>> ReturnAllRivals<TAgent, TContext>(AIAgentType aiAgentType)
        where TAgent : AICombatAgentEntity<TContext>
        where TContext : AICombatAgentContext
        {
            List<AICombatAgentEntity<TContext>> combatAgents = new List<AICombatAgentEntity<TContext>>();

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