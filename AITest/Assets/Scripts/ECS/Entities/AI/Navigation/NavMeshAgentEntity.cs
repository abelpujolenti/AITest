using System.Collections;
using AI;
using ECS.Components.AI.Navigation;
using Interfaces.AI.Navigation;
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

        protected float _rotationSpeed;

        protected bool _isRotating;

        private void Awake()
        {
            Setup();
        }

        protected void Setup()
        {
            Transform ownTransform = transform;
            _navMeshAgentComponent = new NavMeshAgentComponent(_navMeshAgentSpecs, _navMeshAgent, ownTransform);
            _positionComponent = new VectorComponent(ownTransform.position);
            _rotationSpeed = _navMeshAgentSpecs.rotationSpeed;
        }

        public void ContinueNavigation()
        {
            _navMeshAgentComponent.GetNavMeshAgent().isStopped = false;
        }

        public void StopNavigation()
        {
            _navMeshAgentComponent.GetNavMeshAgent().isStopped = true;
        }

        public void RotateToGivenPosition(Vector3 position)
        {
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

            StartCoroutine(EnsureAPathExists());
        }

        private IEnumerator EnsureAPathExists()
        {
            while (_navMeshAgent.path.corners.Length <= 0)
            {
                yield return null;
            }

            StartCoroutine(RotateToGivenPositionCoroutine(_navMeshAgent.path.corners[1]));
        }

        protected virtual IEnumerator RotateToGivenPositionCoroutine(Vector3 position)
        {
            Transform ownTransform = transform;
            
            Vector3 vectorToNextPathCorner = position - ownTransform.position;
            vectorToNextPathCorner.y = 0;

            while (Vector3.Angle(ownTransform.forward, vectorToNextPathCorner) >= 10f)
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
