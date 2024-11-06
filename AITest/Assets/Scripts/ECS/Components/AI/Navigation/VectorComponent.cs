using Interfaces.AI.Navigation;
using UnityEngine;

namespace ECS.Components.AI.Navigation
{
    public class VectorComponent : IPosition
    {
        private Vector3 _position;

        public VectorComponent(Vector3 position)
        {
            _position = position;
        }

        public Vector3 GetPosition()
        {
            return _position;
        }

        public void SetPosition(Vector3 position)
        {
            _position = position;
        }
    }
}
