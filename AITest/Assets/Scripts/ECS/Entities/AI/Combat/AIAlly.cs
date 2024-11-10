using System;
using System.Collections;
using System.Collections.Generic;
using AI;
using AI.Combat;
using AI.Combat.Ally;
using AI.Combat.ScriptableObjects;
using ECS.Components.AI.Combat;
using ECS.Components.AI.Navigation;
using Interfaces.AI.Combat;
using Managers;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace ECS.Entities.AI.Combat
{
    public class AIAlly : AICombatAgentEntity<AIAllyContext, AllyAttackComponent, DamageComponent>
    {
        [SerializeField] private AIAllySpecs _aiAllySpecs;

        private List<uint> _threatGroupsThatThreatMe = new List<uint>();

        private List<AIEnemyAttackCollider> _oncomingEnemyAttacks = new List<AIEnemyAttackCollider>();

        private Dictionary<AllyAttackComponent, AIAttackCollider> _attacksColliders =
            new Dictionary<AllyAttackComponent, AIAttackCollider>();
        
        private uint[] _threatGroupsThatFightAllies = Array.Empty<uint>();
        
        private MoralComponent _moralComponent;
        
        private DieComponent _dieComponent;

        private float _faintDuration;

        private Coroutine _faintCoroutine; 

        private void Start()
        {
            Setup();
            SetupCombatComponents(_aiAllySpecs);
            InstantiateAttackComponents(_aiAllySpecs.aiAttacks);
            CalculateMinimumAndMaximumRangeToAttacks(_attackComponents);

            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            
            _moralComponent = new MoralComponent(_aiAllySpecs.moralWeight);
            _dieComponent = new DieComponent();
            _faintDuration = _aiAllySpecs.faintDuration;
            _context = new AIAllyContext(_aiAllySpecs.totalHealth, capsuleCollider.radius, 
                _aiAllySpecs.sightMaximumDistance, _minimumRangeToCastAnAttack, _maximumRangeToCastAnAttack, 
                transform, capsuleCollider.height, _aiAllySpecs.moralWeight, _aiAllySpecs.radiusOfAlert);
            
            CombatManager.Instance.AddAIAlly(this);
            
            InstantiateAttacksColliders();
            
            StartUpdate();
        }

        private void InstantiateAttackComponents(List<AIAllyAttack> attacks)
        {
            foreach (AIAllyAttack aiAllyAttack in attacks)
            {
                switch (aiAllyAttack.aiAttackAoEType)
                {
                    case AIAttackAoEType.RECTANGLE_AREA:
                        _attackComponents.Add(new AllyRectangleAttackComponent(aiAllyAttack));
                        break;
                    
                    case AIAttackAoEType.CIRCLE_AREA:
                        _attackComponents.Add(new AllyCircleAttackComponent(aiAllyAttack));
                        break;
                    
                    case AIAttackAoEType.CONE_AREA:
                        _attackComponents.Add(new AllyConeAttackComponent(aiAllyAttack));
                        break;
                }
            }
        }

        private void InstantiateAttacksColliders()
        {
            int layerTarget = GameManager.Instance.GetEnemyLayer();
            
            foreach (AllyAttackComponent attackComponent in _attackComponents)
            {
                GameObject colliderObject = new GameObject();

                switch (attackComponent.GetAIAttackAoEType())
                {
                    case AIAttackAoEType.RECTANGLE_AREA:
                        AIAllyRectangleAttackCollider rectangleAttackCollider =
                            colliderObject.AddComponent<AIAllyRectangleAttackCollider>();
                        
                        rectangleAttackCollider.SetRectangleAttackComponent((AllyRectangleAttackComponent)attackComponent);
                        rectangleAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _attacksColliders.Add(attackComponent, rectangleAttackCollider);
                        break;

                    case AIAttackAoEType.CIRCLE_AREA:
                        AIAllyCircleAttackCollider circleAttackCollider =
                            colliderObject.AddComponent<AIAllyCircleAttackCollider>();
                        
                        circleAttackCollider.SetCircleAttackComponent((AllyCircleAttackComponent)attackComponent);
                        circleAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _attacksColliders.Add(attackComponent, circleAttackCollider);
                        break;

                    case AIAttackAoEType.CONE_AREA:
                        AIAllyConeAttackCollider coneAttackCollider = colliderObject.AddComponent<AIAllyConeAttackCollider>();
                        coneAttackCollider.SetConeAttackComponent((AllyConeAttackComponent)attackComponent);
                        coneAttackCollider.SetAttackTargets((int)Mathf.Pow(2, layerTarget));
                        _attacksColliders.Add(attackComponent, coneAttackCollider);
                        break;
                }

                colliderObject.SetActive(false);
            }
        }

        protected override IEnumerator UpdateCoroutine()
        {
            while (true)
            {
                UpdateVisibleRivals();

                UpdateVectorToRival();

                UpdateDistancesToThreatGroupsThatThreatMe();

                if (_context.IsAttacking())
                {
                    yield return null;
                    continue;
                }

                if (_isRotating)
                {
                    yield return null;
                    continue;
                }

                float angleToDestination = Vector3.Angle(transform.forward,
                    GetNavMeshAgentComponent().GetNavMeshAgent().destination - transform.position);

                NavMeshAgent navMeshAgent = GetNavMeshAgentComponent().GetNavMeshAgent();

                Vector3 destination = navMeshAgent.destination;
                    
                /*if (Vector3.Distance(transform.position, destination) < _stoppingDistance && angleToDestination > 15f)
                {
                    RotateToGivenPosition(destination);
                    yield return null;
                    continue;
                }*/

                /*if (Vector3.Angle(transform.forward, navMeshAgent.path.corners[1] - transform.position) > 120f)
                {
                    RotateToNextPathCorner();
                    yield return null;
                    continue;
                }*/
            
                CalculateBestAction();

                yield return null;
            }
        }

        public ref MoralComponent GetMoralComponent()
        {
            return ref _moralComponent;
        }
        
        public DieComponent GetDieComponent()
        {
            return _dieComponent;
        }

        protected override void UpdateVisibleRivals()
        {
            _visibleRivals = CombatManager.Instance.GetVisibleRivals
                <AIEnemy, AIEnemyContext, AttackComponent, AllyDamageComponent, 
                    AIAllyContext, AllyAttackComponent, DamageComponent>(this);

            _context.SetIsSeeingARival(_visibleRivals.Count != 0);

            if (_visibleRivals.Count == 0)
            {
                return;
            }

            _threatGroupsThatThreatMe = CombatManager.Instance.FilterThreatGroupsThatThreatMe(GetCombatAgentInstance(), 
                GetStatWeightComponent(), _context.GetThreatGroupOfTarget(), _visibleRivals);

            _threatGroupsThatFightAllies = CombatManager.Instance.FilterPerThreatGroupAlliesFighting(this);
        }

        private void UpdateDistancesToThreatGroupsThatThreatMe()
        {
            _context.SetDistancesToThreatGroupsThatThreatMe(
                CombatManager.Instance.GetDistancesToGivenThreatGroups(transform.position, _threatGroupsThatThreatMe));
        }

        protected override void CalculateBestAction()
        {
            CombatManager.Instance.CalculateBestAction(this);
        }

        public override void OnAttackAvailableAgain(AllyAttackComponent attackComponent)
        {
            base.OnAttackAvailableAgain(attackComponent);

            _context.SetCanDefeatEnemy(CalculateIfGivenAttackCanDefeatEnemy(attackComponent));
            _context.SetCanStunEnemy(CalculateIfGivenAttackCanStunEnemy(attackComponent));
        }

        private void CalculateIfCanDefeatEnemy()
        {
            bool canDefeat;
            
            foreach (AllyAttackComponent allyAttackComponent in _attackComponents)
            {
                if (allyAttackComponent.IsOnCooldown())
                {
                    continue;
                }

                canDefeat = CalculateIfGivenAttackCanDefeatEnemy(allyAttackComponent);

                if (!canDefeat)
                {
                    continue;
                }
                
                _context.SetCanDefeatEnemy(true);
                return;
            }
            
            _context.SetCanDefeatEnemy(false);
        }

        private bool CalculateIfGivenAttackCanDefeatEnemy(AllyAttackComponent allyAttackComponent)
        {
            return allyAttackComponent.GetDamage() >= _context.GetRivalHealth();
        }

        private void CalculateIfCanStunEnemy()
        {
            bool canStun;
            
            foreach (AllyAttackComponent allyAttackComponent in _attackComponents)
            {
                if (allyAttackComponent.IsOnCooldown())
                {
                    continue;
                }

                canStun = CalculateIfGivenAttackCanStunEnemy(allyAttackComponent);

                if (!canStun)
                {
                    continue;
                }
                
                _context.SetCanStunEnemy(true);
                return;
            }
            
            _context.SetCanStunEnemy(false);
        }

        private bool CalculateIfGivenAttackCanStunEnemy(AllyAttackComponent allyAttackComponent)
        {
            return !(allyAttackComponent.GetStressDamage() + _context.GetRivalCurrentStress() < 
                     _context.GetRivalMaximumStress());
        }

        public void Attack()
        {
            AllyAttackComponent attackComponent = ReturnNextAttack();
            
            _context.SetIsAttacking(true);

            StartCastingAnAttack(attackComponent);
        }

        public List<Vector2> GetOncomingEnemiesAttacksCorners()
        {
            List<Vector2> originalPolygon = new List<Vector2>();
            List<Vector2> newPolygon = new List<Vector2>();
            
            originalPolygon.AddRange(_oncomingEnemyAttacks[0].GetCornerPoints());

            for (int i = 0; i < _oncomingEnemyAttacks.Count - 1; i++)
            {
                newPolygon.AddRange(_oncomingEnemyAttacks[i + 1].GetCornerPoints());

                originalPolygon = PolygonUtilities.Union2Polygons(originalPolygon, newPolygon);
                
                newPolygon.Clear();
            }

            return originalPolygon;
        }

        public void DodgeAttack(VectorComponent positionToDodge)
        {
            ContinueNavigation();

            NavMeshAgentComponent navMeshAgentComponent = GetNavMeshAgentComponent();

            navMeshAgentComponent.GetNavMeshAgent().stoppingDistance = 1;
            
            ECSNavigationManager.Instance.UpdateNavMeshAgentVectorDestination(GetNavMeshAgentComponent(),positionToDodge);
        }

        private void StartCastingAnAttack(AllyAttackComponent allyAttackComponent)
        {
            if (allyAttackComponent.IsOnCooldown())
            {
                GetContext().SetIsAttacking(false);
                return;
            }
            
            AIAttackCollider attackCollider = _attacksColliders[allyAttackComponent];
            
            attackCollider.SetParent(transform);
            StartCoroutine(StartAttackCastTimeCoroutine(allyAttackComponent, attackCollider));
        }

        private void PutAttackOnCooldown(AllyAttackComponent attackComponent)
        {
            StartCoroutine(StartCooldownCoroutine(attackComponent));
        }

        private IEnumerator StartAttackCastTimeCoroutine(AllyAttackComponent allyAttackComponent, 
            AIAttackCollider attackCollider)
        {
            allyAttackComponent.StartCastTime();
            
            attackCollider.gameObject.SetActive(true);
            
            while (allyAttackComponent.IsCasting())
            {
                allyAttackComponent.DecreaseCurrentCastTime();
                yield return null;
            }
            
            attackCollider.gameObject.SetActive(true);

            yield return null;
            
            attackCollider.StartInflictingDamage();

            if (allyAttackComponent.DoesDamageOverTime())
            {
                StartCoroutine(StartDamageOverTime(allyAttackComponent, attackCollider));
                yield break;
            }
            
            RotateToNextPathCorner();
            PutAttackOnCooldown(allyAttackComponent);
            attackCollider.Deactivate();
        }

        private IEnumerator StartDamageOverTime(AllyAttackComponent allyAttackComponent, 
            AIAttackCollider attackCollider)
        {
            while (allyAttackComponent.DidDamageOverTimeFinished())
            {
                allyAttackComponent.DecreaseRemainingTimeDealingDamage();
                yield return null;
            }
           
            RotateToNextPathCorner();
            PutAttackOnCooldown(allyAttackComponent);
            attackCollider.Deactivate();
        }

        private IEnumerator StartCooldownCoroutine(AllyAttackComponent allyAttackComponent)
        {
            allyAttackComponent.StartCooldown();
            while (allyAttackComponent.IsOnCooldown())
            {
                allyAttackComponent.DecreaseCooldown();
                yield return null;
            }
            
            OnAttackAvailableAgain(allyAttackComponent);
        }

        public void WarnOncomingDamage(RectangleAttackComponent rectangleAttackComponent, AIEnemyAttackCollider enemyAttackCollider)
        {
            _oncomingEnemyAttacks.Add(enemyAttackCollider);
            
            _context.SetIsUnderAttack(true);
            _context.SetOncomingAttackDamage(_context.GetOncomingAttackDamage() + rectangleAttackComponent.GetDamage());
        }
        
        public void WarnOncomingDamage(CircleAttackComponent circleAttackComponent, AIEnemyAttackCollider enemyAttackCollider)
        {
            _oncomingEnemyAttacks.Add(enemyAttackCollider);
            
            _context.SetIsUnderAttack(true);
            _context.SetOncomingAttackDamage(_context.GetOncomingAttackDamage() + circleAttackComponent.GetDamage());
        }
        
        public void WarnOncomingDamage(ConeAttackComponent coneAttackComponent, AIEnemyAttackCollider enemyAttackCollider)
        {
            _oncomingEnemyAttacks.Add(enemyAttackCollider);
            
            _context.SetIsUnderAttack(true);
            _context.SetOncomingAttackDamage(_context.GetOncomingAttackDamage() + coneAttackComponent.GetDamage());
        }

        public void FreeOfWarnArea(RectangleAttackComponent rectangleAttackComponent, AIEnemyAttackCollider enemyAttackCollider)
        {
            _oncomingEnemyAttacks.Remove(enemyAttackCollider);
            
            _context.SetOncomingAttackDamage(_context.GetOncomingAttackDamage() - rectangleAttackComponent.GetDamage());
            CheckIfOutOfDanger();
        }

        public void FreeOfWarnArea(CircleAttackComponent circleAttackComponent, AIEnemyAttackCollider enemyAttackCollider)
        {
            _oncomingEnemyAttacks.Remove(enemyAttackCollider);
            
            _context.SetOncomingAttackDamage(_context.GetOncomingAttackDamage() - circleAttackComponent.GetDamage());
            CheckIfOutOfDanger();
        }

        public void FreeOfWarnArea(ConeAttackComponent coneAttackComponent, AIEnemyAttackCollider enemyAttackCollider)
        {
            _oncomingEnemyAttacks.Remove(enemyAttackCollider);
            
            _context.SetOncomingAttackDamage(_context.GetOncomingAttackDamage() - coneAttackComponent.GetDamage());
            CheckIfOutOfDanger();
        }

        private void CheckIfOutOfDanger()
        {
            _context.SetIsUnderAttack(_oncomingEnemyAttacks.Count != 0);

            if (_oncomingEnemyAttacks.Count != 0)
            {
                return;
            }

            NavMeshAgentComponent navMeshAgentComponent = GetNavMeshAgentComponent();

            navMeshAgentComponent.GetNavMeshAgent().stoppingDistance = 7;

            if (_lastDestination != null)
            {
                ECSNavigationManager.Instance.UpdateNavMeshAgentVectorDestination(GetNavMeshAgentComponent(), _lastDestination);
                return;
            }
            
            ECSNavigationManager.Instance.UpdateNavMeshAgentTransformDestination(GetNavMeshAgentComponent(), 
                new TransformComponent(GetContext().GetRivalTransform()));
        }

        public override void OnReceiveDamage(DamageComponent damageComponent)
        {
            _context.SetHealth(_context.GetHealth() - damageComponent.GetDamage());

            if (_context.GetHealth() != 0)
            {
                StartCoroutine(DamageFeedback());
                return;
            }
            
            OnDefeated();
        }

        protected override void OnDefeated()
        {
            CombatManager.Instance.OnAllyDefeated(this);
            _faintCoroutine = StartCoroutine(FaintDurationCoroutine());
        }

        private IEnumerator FaintDurationCoroutine()
        {
            float currentTime = 0;

            while (currentTime < _faintDuration)
            {
                currentTime += Time.deltaTime;
                yield return null;
            }
            
            OnDie();
        }

        private void OnDie()
        {
            ECSNavigationManager.Instance.RemoveNavMeshAgentEntity(GetNavMeshAgentComponent());
            Destroy(gameObject);
        }

        private void OnBeingRescued()
        {
            StopCoroutine(_faintCoroutine);
        }

        public override AIAgentType GetAIAgentType()
        {
            return _aiAllySpecs.aiAgentType;
        }

        public override AIAllyContext GetContext()
        {
            return _context;
        }

        public override void SetLastActionIndex(uint lastActionIndex)
        {
            _context.SetLastActionIndex(lastActionIndex);
        }

        public override void SetHealth(uint health)
        {
            _context.SetHealth(health);
        }

        public override void SetRivalIndex(uint rivalIndex)
        {
            _context.SetRivalIndex(rivalIndex);
        }

        public override void SetRivalRadius(float rivalRadius)
        {
            _context.SetRivalRadius(rivalRadius);
        }

        public override void SetDistanceToRival(float distanceToRival)
        {
            _context.SetDistanceToRival(distanceToRival);
        }

        public override void SetIsSeeingARival(bool isSeeingARival)
        {
            _context.SetIsSeeingARival(isSeeingARival);
        }

        public override void SetHasATarget(bool hasATarget)
        {
            _context.SetHasATarget(hasATarget);
        }

        public override void SetIsFighting(bool isFighting)
        {
            _context.SetIsFighting(isFighting);
        }

        public override void SetIsAttacking(bool isAttacking)
        {
            _context.SetIsAttacking(isAttacking);
        }

        public override void SetVectorToRival(Vector3 vectorToRival)
        {
            _context.SetVectorToRival(vectorToRival);
        }

        public override void SetRivalTransform(Transform rivalTransform)
        {
            _context.SetRivalTransform(rivalTransform);
        }

        public override IStatWeight GetStatWeightComponent()
        {
            return _moralComponent;
        }

        public void SetOncomingAttackDamage(uint oncomingAttackDamage)
        {
            _context.SetOncomingAttackDamage(oncomingAttackDamage);
        }

        public void SetEnemyHealth(uint enemyHealth)
        {
            _context.SetRivalHealth(enemyHealth);
            CalculateIfCanDefeatEnemy();
        }

        public void SetThreatGroupOfTarget(uint threatGroupOfTarget)
        {
            _context.SetThreatGroupOfTarget(threatGroupOfTarget);
        }

        public void SetMoralWeight(float moralWeight)
        {
            _context.SetMoralWeight(moralWeight);
        }

        public void SetThreatWeightOfTarget(float threatWeightOfTarget)
        {
            _context.SetThreatWeightOfTarget(threatWeightOfTarget);
        }

        public void SetEnemyMaximumStress(float enemyMaximumStress)
        {
            _context.SetRivalMaximumStress(enemyMaximumStress);
        }

        public void SetEnemyCurrentStress(float enemyCurrentStress)
        {
            _context.SetRivalCurrentStress(enemyCurrentStress);
            CalculateIfCanStunEnemy();
        }

        public void SetIsEnemyStunned(bool isEnemyStunned)
        {
            _context.SetIsEnemyStunned(isEnemyStunned);
        }

        public void SetIsUnderThreat(bool isUnderThreat)
        {
            _context.SetIsUnderThreat(isUnderThreat);
        }

        public void SetIsUnderAttack(bool isUnderAttack)
        {
            _context.SetIsUnderAttack(isUnderAttack);
        }

        public void SetIsAnotherAllyUnderThreat(bool isAnotherAllyUnderThreat)
        {
            _context.SetIsAnotherAllyUnderThreat(isAnotherAllyUnderThreat);
        }

        public void SetIsAirborne(bool isAirborne)
        {
            _context.SetIsAirborne(isAirborne);
        }

        public void SetState(AIAllyOrders allyOrder)
        {
            _context.SetIsInRetreatState(allyOrder == AIAllyOrders.RETREAT);
            _context.SetIsInAttackState(allyOrder == AIAllyOrders.ATTACK);
            _context.SetIsInFleeState(allyOrder == AIAllyOrders.FLEE);
        }

        public List<AllyAttackComponent> GetAllyAttackComponents()
        {
            return _attackComponents;
        }

        public List<uint> GetThreatGroupsThatThreatMe()
        {
            return _threatGroupsThatThreatMe;
        }

        public uint[] GetThreatGroupsThatFightAllies()
        {
            return _threatGroupsThatFightAllies;
        }
    }
}