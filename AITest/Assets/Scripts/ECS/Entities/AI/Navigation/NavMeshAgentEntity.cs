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

        private NavMeshAgentComponent _navMeshAgentComponent;

        private IPosition _positionComponent;

        private void Awake()
        {
            Setup();
        }

        protected void Setup()
        {
            Transform ownTransform = transform;
            _navMeshAgentComponent = new NavMeshAgentComponent(_navMeshAgentSpecs, GetComponent<NavMeshAgent>(), ownTransform);
            _positionComponent = new VectorComponent(ownTransform.position);
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
