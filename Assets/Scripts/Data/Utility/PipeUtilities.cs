using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility
{
    public static class PipeUtilities
    {
        public static Vector3 GetMiteredPoint(Vector3 start, Vector3 end, float distance)
        {
            return start + Vector3.ClampMagnitude(end - start, distance);
        }

        public static Vector2[] GetRegularPolygon(int sides, float radius, float angleOffset = 0)
        {
            var points = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                points[i] = Quaternion.AngleAxis(((i / (float)sides) * 360f) + angleOffset, Vector3.forward) *
                            (Vector2.right * radius);
            }

            return points;
        }

        public static Vector3[] GetPipeCrossSection(Vector3 position, Quaternion direction, int sides, float radius)
        {
            var basePoints = GetRegularPolygon(sides, radius, (0.5f / sides) * 360);
            var outPoints = basePoints.Select(point => (direction * point) + position).ToArray();
            return outPoints;
        }

        public static List<Vector3Int> SimplifyPath(Vector3Int[] path)
        {
            var outPath = new List<Vector3Int>();
            Vector3Int lastDirection = Vector3Int.zero;
            if (path.Length < 3)
            {
                return new List<Vector3Int>(path);
            }

            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3Int firstPosition = path[i];
                Vector3Int nextPosition = path[i + 1];
                Vector3Int thisDirection = nextPosition - firstPosition;
                Vector3Int oldLastDirection = lastDirection;
                lastDirection = nextPosition - firstPosition;
                if (Vector3.Angle(thisDirection, oldLastDirection) <= 0 && oldLastDirection != Vector3.zero &&
                    i != path.Length - 2)
                {
                    continue;
                }

                outPath.Add(firstPosition);
            }

            outPath.Add(path[^1]);
            return outPath;
        }
    }
}