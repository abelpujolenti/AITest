using Interfaces.AI.Navigation;
using UnityEngine;

namespace ECS.Components.AI.Navigation
{
    public class TransformComponent : IPosition
    {
        private Transform _transform;

        public TransformComponent(Transform transform)
        {
            _transform = transform;
        }

        public Transform GetTransform()
        {
            return _transform;
        }

        public Vector3 GetPosition()
        {
            return _transform.position;
        }
    }
}
