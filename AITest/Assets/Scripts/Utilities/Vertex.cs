using UnityEngine;

namespace Utilities
{
    public class Vertex
    {
        public Vector3 position;

        public HalfEdge halfEdge;

        public Triangle triangle;
        
        public Vertex previousVertex;
        public Vertex nextVertex;

        public Vertex(Vector3 position)
        {
            this.position = position;
        }

        public Vector2 GetXZPosition()
        {
            return new Vector2(position.x, position.z);
        }

    }
}
