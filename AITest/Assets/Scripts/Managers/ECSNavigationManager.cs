using System.Collections.Generic;
using ECS.Components.AI.Navigation;
using ECS.Systems.AI.Navigation;
using Interfaces.AI.Navigation;
using UnityEngine;

namespace Managers
{
    public class ECSNavigationManager : MonoBehaviour
    {
        private static ECSNavigationManager _instance;

        public static ECSNavigationManager Instance => _instance;
        
        private Dictionary<NavMeshAgentComponent, IPosition> _navMeshAgentDestinations = 
            new Dictionary<NavMeshAgentComponent, IPosition>();

        private UpdateAgentDestinationSystem _updateAgentDestinationSystem = new UpdateAgentDestinationSystem();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                
                DontDestroyOnLoad(gameObject);
                
                return;
            }
            
            Destroy(gameObject);
        }

        private void Update()
        {
            foreach (var navMeshAgentDestination in _navMeshAgentDestinations)
            {
                if (navMeshAgentDestination.Value == null)
                {
                    continue;
                }
                _updateAgentDestinationSystem.UpdateAgentDestination(navMeshAgentDestination.Key, navMeshAgentDestination.Value);
            }
        }

        public void AddNavMeshAgentEntity(NavMeshAgentComponent navMeshAgentComponent)
        {
            _navMeshAgentDestinations.Add(navMeshAgentComponent, null);
        }

        public void UpdateNavMeshAgentVectorDestination(NavMeshAgentComponent navMeshAgentComponent, 
            VectorComponent vectorComponent)
        {
            _navMeshAgentDestinations[navMeshAgentComponent] = vectorComponent;
        }

        public void UpdateNavMeshAgentTransformDestination(NavMeshAgentComponent navMeshAgentComponent,
            TransformComponent transformComponent)
        {
            _navMeshAgentDestinations[navMeshAgentComponent] = transformComponent;
        }
    }
}
