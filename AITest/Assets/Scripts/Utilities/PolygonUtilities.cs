using System.Collections.Generic;
using ClipperLib;
using UnityEngine;

namespace Utilities
{
    public static class PolygonUtilities
    {
        public static List<Vector2> Union2Polygons(List<Vector2> polygon1, List<Vector2> polygon2)
        {
            const int scaleFactor = 1000000;
            List<IntPoint> clipperPoly1 = ConvertToClipperPath(polygon1, scaleFactor);
            List<IntPoint> clipperPoly2 = ConvertToClipperPath(polygon2, scaleFactor);

            Clipper clipper = new Clipper();
            clipper.AddPath(clipperPoly1, PolyType.ptSubject, true);
            clipper.AddPath(clipperPoly2, PolyType.ptClip, true);

            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            return ConvertToUnityPath(solution[0], scaleFactor);
        }

        private static List<IntPoint> ConvertToClipperPath(List<Vector2> polygon, int scaleFactor)
        {
            List<IntPoint> result = new List<IntPoint>();
            foreach (var point in polygon)
            {
                result.Add(new IntPoint(point.x * scaleFactor, point.y * scaleFactor));
            }
            return result;
        }

        private static List<Vector2> ConvertToUnityPath(List<IntPoint> polygon, int scaleFactor)
        {
            List<Vector2> result = new List<Vector2>();
            foreach (var point in polygon)
            {
                result.Add(new Vector2((float)point.X / scaleFactor, (float)point.Y / scaleFactor));
            }
            return result;
        }
        
        public static List<Vector2> OrderVerticesCounterClockwise(List<Vector2> vertices)
        {
            Vector2 centroid = GetCentroid(vertices);

            vertices.Sort((a, b) => CompareAngle(a, b, centroid));

            return vertices;
        }

        public static List<Vector2> OrderVerticesClockwise(List<Vector2> vertices)
        {
            Vector2 centroid = GetCentroid(vertices);

            vertices.Sort((a, b) => CompareAngle(b, a, centroid));

            return vertices;
        }
        
        private static Vector2 GetCentroid(List<Vector2> vertices)
        {
            Vector2 centroid = Vector2.zero;
            foreach (Vector2 vertex in vertices)
            {
                centroid += vertex;
            }
            centroid /= vertices.Count;
            return centroid;
        }
        
        private static int CompareAngle(Vector2 a, Vector2 b, Vector2 centroid)
        {
            float angleA = Mathf.Atan2(a.y - centroid.y, a.x - centroid.x);
            float angleB = Mathf.Atan2(b.y - centroid.y, b.x - centroid.x);
            return angleA.CompareTo(angleB);
        }
        
        public static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            int n = polygon.Length;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 v1 = polygon[i];
                Vector2 v2 = polygon[j];

                if (((v1.y > point.y) != (v2.y > point.y)) &&
                    (point.x < (v2.x - v1.x) * (point.y - v1.y) / (v2.y - v1.y) + v1.x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
        
        public static Vector2? GetLineIntersection(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float denominator = (p1.x - p2.x) * (q1.y - q2.y) - (p1.y - p2.y) * (q1.x - q2.x);

            if (denominator == 0)
            {
                return null;
            }

            float t = ((p1.x - q1.x) * (q1.y - q2.y) - (p1.y - q1.y) * (q1.x - q2.x)) / denominator;
            float s = ((p1.x - q1.x) * (p1.y - p2.y) - (p1.y - q1.y) * (p1.x - p2.x)) / denominator;

            if (t >= 0 && t <= 1 && s >= 0 && s <= 1)
            {
                Vector2 intersection = p1 + t * (p2 - p1);
                return intersection;
            }

            return null;
        }
    }
}