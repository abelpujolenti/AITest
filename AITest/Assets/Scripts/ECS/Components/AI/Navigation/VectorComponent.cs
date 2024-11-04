using Interfaces.AI.Navigation;
using UnityEngine;

namespace ECS.Components.AI.Navigation
{
    public class VectorComponent : IPosition
    {
        private Vector3 _destination;

        public VectorComponent(Vector3 destination)
        {
            _destination = destination;
        }

        public Vector3 GetPosition()
        {
            return _destination;
        }

        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
        }
    }
}
