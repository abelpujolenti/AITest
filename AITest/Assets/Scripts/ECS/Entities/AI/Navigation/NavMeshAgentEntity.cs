using System;
using System.Collections;
using AI;
using ECS.Components.AI.Navigation;
using Interfaces.AI.Navigation;
using Managers;
using UnityEngine;
using UnityEngine.AI;

namespace ECS.Entities.AI.Navigation
{
    public class NavMeshAgentEntity : MonoBehaviour
    {
        [SerializeField] protected NavMeshAgentSpecs _navMeshAgentSpecs;

        [SerializeField] protected NavMeshAgent _navMeshAgent;

        private NavMeshAgentComponent _navMeshAgentComponent;

        private IPosition _positionComponent;

        protected float _stoppingDistance = 7;

        protected float _rotationSpeed;

        protected bool _isRotating;

        protected void Setup()
        {
            Transform ownTransform = transform;
            _navMeshAgentComponent = new NavMeshAgentComponent(_navMeshAgentSpecs, _navMeshAgent, ownTransform);
            _positionComponent = new VectorComponent(ownTransform.position);
            _rotationSpeed = _navMeshAgentSpecs.rotationSpeed;
            //ECSNavigationManager.Instance.AddNavMeshAgentEntity(_navMeshAgentComponent);
        }

        public void ContinueNavigation()
        {
            _navMeshAgentComponent.GetNavMeshAgent().isStopped = false;
        }

        public void StopNavigation()
        {
            _navMeshAgentComponent.GetNavMeshAgent().isStopped = true;
        }

        public void Rotate()
        {
            NavMeshAgentComponent navMeshAgentComponent = GetNavMeshAgentComponent();
            
            float angleToDestination = Vector3.Angle(transform.forward,
                navMeshAgentComponent.GetNavMeshAgent().destination - transform.position);

            NavMeshAgent navMeshAgent = GetNavMeshAgentComponent().GetNavMeshAgent();

            Vector3 destination = ECSNavigationManager.Instance.GetNavMeshAgentDestination(navMeshAgentComponent)
                .GetPosition();

            Transform ownTransform = transform;

            Vector3 position = ownTransform.position;
                    
            if (Vector3.Distance(position, destination) < _stoppingDistance && angleToDestination > 15f)
            {
                RotateToGivenPosition(destination);
                return;
            }

            StartCoroutine(EnsureAPathExists(() =>
                Vector3.Angle(ownTransform.forward, navMeshAgent.path.corners[1] - position) > 120f));
        }

        private void RotateToGivenPosition(Vector3 position)
        {
            if (_isRotating)
            {
                return;
            }
            
            StopNavigation();

            _isRotating = true;
            
            StartCoroutine(RotateToGivenPositionCoroutine(position));
        }

        public void RotateToNextPathCorner()
        {
            if (_isRotating)
            {
                return;
            }
            
            StopNavigation();

            _isRotating = true;

            StartCoroutine(EnsureAPathExists(()=> true));
        }

        private IEnumerator EnsureAPathExists(Func<bool> action)
        {
            while (_navMeshAgent.path.corners.Length <= 0)
            {
                yield return null;
            }

            if (!action())
            {
                yield break;
            }

            StartCoroutine(RotateToGivenPositionCoroutine(_navMeshAgent.path.corners[1]));
        }

        protected virtual IEnumerator RotateToGivenPositionCoroutine(Vector3 position)
        {
            Transform ownTransform = transform;
            
            Vector3 vectorToNextPathCorner = position - ownTransform.position;
            vectorToNextPathCorner.y = 0;

            while (Vector3.Angle(ownTransform.forward, vectorToNextPathCorner) >= 5f)
            {
                Quaternion rotation = Quaternion.LookRotation(vectorToNextPathCorner);
                ownTransform.rotation = Quaternion.Slerp(ownTransform.rotation, rotation, _rotationSpeed * Time.deltaTime);
                yield return null;
            }
            
            _isRotating = false;
            
            ContinueNavigation();
        }

        public NavMeshAgentComponent GetNavMeshAgentComponent()
        {
            return _navMeshAgentComponent;
        }

        public IPosition GetDestinationComponent()
        {
            return _positionComponent;
        }
    }
}
