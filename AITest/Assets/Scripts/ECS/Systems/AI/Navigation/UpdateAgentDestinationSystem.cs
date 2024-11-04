using ECS.Components.AI.Navigation;
using Interfaces.AI.Navigation;

namespace ECS.Systems.AI.Navigation
{
    public class UpdateAgentDestinationSystem
    {
        public void UpdateAgentDestination(NavMeshAgentComponent navMeshAgentComponent, IPosition positionComponent)
        {
            navMeshAgentComponent.GetNavMeshAgent().SetDestination(positionComponent.GetPosition());
        }
    }
}
