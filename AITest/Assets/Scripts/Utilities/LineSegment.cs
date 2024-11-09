using UnityEngine;

namespace Utilities
{
    public class LineSegment
    {
        public Vector2 start;
        public Vector2 end;

        public LineSegment(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }
    }
}