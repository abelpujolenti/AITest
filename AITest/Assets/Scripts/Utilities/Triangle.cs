using UnityEngine;

namespace Utilities
{
    public class Triangle
    {
        public Vertex vertex1;
        public Vertex vertex2;
        public Vertex vertex3;

        public HalfEdge halfEdge;

        public Triangle(Vertex vertex1, Vertex vertex2, Vertex vertex3)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
            this.vertex3 = vertex3;
        }

        public Triangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            this.vertex1 = new Vertex(vertex1);
            this.vertex2 = new Vertex(vertex2);
            this.vertex3 = new Vertex(vertex3);
        }

        public Triangle(HalfEdge halfEdge)
        {
            this.halfEdge = halfEdge;
        }

        public void ChangeOrientation()
        {
            (vertex1, vertex2) = (vertex2, vertex1);
        }
    }
}
