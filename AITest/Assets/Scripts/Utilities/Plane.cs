using UnityEngine;

namespace Utilities
{
    public class Plane
    {
        public Vector3 position;
        public Vector3 normal;

        public Plane(Vector3 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal;
        }
    }
}