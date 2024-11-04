using UnityEngine;

namespace Utilities
{
    public class Edge
    {
        public Vertex vertex1;
        public Vertex vertex2;

        public bool isIntersecting;

        public Edge(Vertex vertex1, Vertex vertex2)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
        }

        public Edge(Vector3 vertex1, Vector3 vertex2)
        {
            this.vertex1 = new Vertex(vertex1);
            this.vertex2 = new Vertex(vertex2);
        }

        public Vector2 GetXZPosition(Vertex vertex)
        {
            return new Vector2(vertex.position.x, vertex.position.z);
        }

        public void FlipEdge()
        {
            (vertex1, vertex2) = (vertex2, vertex1);
        }
    }
}