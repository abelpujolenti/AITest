namespace Utilities
{
    public class HalfEdge
    {
        public Vertex vertex;

        public Triangle triangle;

        public HalfEdge nextEdge;
        public HalfEdge previousEdge;
        public HalfEdge oppositeEdge;

        public HalfEdge(Vertex vertex)
        {
            this.vertex = vertex;
        }
    }
}
