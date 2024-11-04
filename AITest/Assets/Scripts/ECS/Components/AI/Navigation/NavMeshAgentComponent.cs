using System;
using AI;
using UnityEngine;
using UnityEngine.AI;

namespace ECS.Components.AI.Navigation
{
    [Serializable]
    public class NavMeshAgentComponent
    {
        private NavMeshAgentSpecs _navMeshAgentSpecs;
        
        private TransformComponent _transformComponent;
        
        private NavMeshAgent _agent;

        public NavMeshAgentComponent(NavMeshAgentSpecs navMeshAgentSpecs, NavMeshAgent agent, Transform transform)
        {
            _navMeshAgentSpecs = navMeshAgentSpecs;
            _agent = agent;
            _transformComponent = new TransformComponent(transform);
        }

        public NavMeshAgent GetNavMeshAgent()
        {
            return _agent;
        }

        public TransformComponent GetTransformComponent()
        {
            return _transformComponent;
        }
    }
}
