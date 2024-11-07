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

        public void RotateToNextPathCorner()
        {
            StopNavigation();

            StartCoroutine(RotateToNextPathCornerCoroutine());
        }

        protected virtual IEnumerator RotateToNextPathCornerCoroutine()
        {
            Vector3 vectorToNextPathCorner = _navMeshAgent.path.corners[1] - transform.position;
            vectorToNextPathCorner.y = 0;

            while (Vector3.Angle(transform.forward, vectorToNextPathCorner) >= 5f)
            {
                Quaternion rotation = Quaternion.LookRotation(vectorToNextPathCorner);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, _rotationSpeed * Time.deltaTime);
                yield return null;
            }
            
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
