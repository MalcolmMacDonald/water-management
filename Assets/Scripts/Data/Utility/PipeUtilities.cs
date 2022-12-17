using System.Collections.Generic;
using System.Linq;
using Pipes;
using UnityEngine;

namespace Utility
{
    public static class PipeUtilities
    {
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