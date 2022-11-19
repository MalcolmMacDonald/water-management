using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Utility
{
    public static class GridUtilities
    {
        private const int NEIGHBORS_ARRAY_SIZE = 26;
        private static Vector3Int[] _neighborOffsets;
        private static int[] cornerCollisionMask;

        public static Vector3Int[] NeighborOffsets
        {
            get
            {
                if (_neighborOffsets == null)
                {
                    _neighborOffsets = GetNeighborOffsets();
                }

                return _neighborOffsets;
            }
        }

        private static Vector3Int[] GetNeighborOffsets()
        {
            var allOffsets = new Vector3Int[NEIGHBORS_ARRAY_SIZE];
            int counter = 0;
            cornerCollisionMask = new int[NEIGHBORS_ARRAY_SIZE];
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    for (int k = -1; k < 2; k++)
                    {
                        if (i == j && j == k && i == 0)
                        {
                            continue;
                        }


                        allOffsets[counter] = new Vector3Int(i, j, k);
                        cornerCollisionMask[counter] = allOffsets[counter].GetCornerCollisionMask();
                        counter++;
                    }
                }
            }

            return allOffsets;
        }

        public static int GetNeighborMask<T>(this IGrid<T> grid, Cell cell) where T : ICell
        {
            int mask = 0;

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                if (grid.HasValue(cell.position + NeighborOffsets[i]))
                {
                    if (cornerCollisionMask[i] != 0)
                    {
                        if (!grid.CheckCorners(cell.position, cornerCollisionMask[i]))
                        {
                            continue;
                        }
                    }

                    mask |= 1 << i;
                }
            }

            return mask;
        }

        public static void UpdateNeighbors(this IGrid<Cell> grid)
        {
            var enumerable = grid.GetEnumerable();
            foreach (Cell cell in enumerable)
            {
                int oldNeighborMask = cell.neighborMask;
                int neighborMask = grid.GetNeighborMask(cell);
                if (neighborMask == oldNeighborMask)
                {
                    continue;
                }

                Cell cellCopy = cell;
                cellCopy.neighborMask = neighborMask;
                cellCopy.neighborCount = cellCopy.neighborMask.MaskFlagCount();
                grid[cell.position] = cellCopy;
            }
        }

        public static void UpdateCosts(this IGrid<Cell> grid)
        {
            var enumerable = grid.GetEnumerable();
            float rayLength = Mathf.Sqrt(3);
            foreach (Cell cell in enumerable)
            {
                Cell cellCopy = cell;

                Vector3 normalDirection = new Vector3();
                for (int i = 0; i < 26; i++)
                {
                    if (cell.MaskHasNeighborIndex(i))
                    {
                        normalDirection += NeighborOffsets[i];
                    }
                }

                normalDirection *= -1;


                if (!Physics.Raycast(cell.position.ToWorld(), normalDirection, rayLength))
                {
                    cellCopy.cost = 3;
                }


                Debug.DrawRay(cell.position.ToWorld(), normalDirection.normalized, Color.red, 1f);


                grid[cell.position] = cellCopy;
            }
        }

        private static bool CheckCorners<T>(this IGrid<T> grid, Vector3Int position, int mask) where T : ICell
        {
            for (int j = 0; j < NEIGHBORS_ARRAY_SIZE; j++)
            {
                if ((mask & (1 << j)) != 0)
                {
                    if (!grid.HasValue(position + NeighborOffsets[j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void DisplayCell(Vector3Int position, Color color)
        {
            var rectPointsCache = new Vector3[5];
            Vector3 worldPosition = position.ToWorld();
            Vector3 sideways = new Vector3(0.5f, 0, 0) * 0.6f;
            Vector3 up = new Vector3(0, 0, 0.5f) * 0.6f;

            rectPointsCache[0] = worldPosition + sideways + up;
            rectPointsCache[1] = (worldPosition + sideways) - up;
            rectPointsCache[2] = worldPosition - sideways - up;
            rectPointsCache[3] = (worldPosition - sideways) + up;
            rectPointsCache[4] = worldPosition + sideways + up;

            for (int j = 0; j < 4; j++)
            {
                Debug.DrawLine(rectPointsCache[j], rectPointsCache[j + 1], color);
            }
        }

        public static T[] Flatten<T>(this T[,,] array)
        {
            var outArray = new T[array.GetLength(0) * array.GetLength(1) * array.GetLength(2)];
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    for (int k = 0; k < array.GetLength(2); k++)
                    {
                        outArray[i + (j * array.GetLength(0)) + (k * array.GetLength(0) * array.GetLength(1))] =
                            array[i, j, k];
                    }
                }
            }

            return outArray;
        }

        public static List<Cell> GetNeighbors(this IGrid<Cell> grid, Cell cell)
        {
            var outNeighbors = new List<Cell>();
            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                if ((cell.neighborMask & (1 << i)) == 0)
                {
                    continue;
                }

                Vector3Int neighborOffset = NeighborOffsets[i];
                outNeighbors.Add(grid[cell.position + neighborOffset]);
            }

            return outNeighbors;
        }

        public static void DisplayCells(IEnumerable<Vector3Int> cellPositions, Color color)
        {
            foreach (Vector3Int position in cellPositions) DisplayCell(position, color);
        }

        private static void DisplayCells(IEnumerable<Vector3Int> cellPositions, double[,,] costs, double min,
            double max,
            Color minColor,
            Color maxColor)
        {
            foreach (Vector3Int position in cellPositions)
                DisplayCell(position,
                    Color.Lerp(minColor, maxColor,
                        (float)((costs[position.x, position.y, position.z] - min) / (max - min))));
        }


        public static void GetNeighborsNonAlloc(this IGrid<Cell> grid, Cell cell, ref Cell[] neighbors,
            ref float[] distances)
        {
            int index = 0;
            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                if ((cell.neighborMask & (1 << i)) == 0)
                {
                    continue;
                }

                Vector3Int neighborOffset = NeighborOffsets[i];
                neighbors[index] = grid[cell.position + neighborOffset];
                distances[index] = neighborOffset.magnitude;
                index++;
            }
        }

        public static void Remove(this ContinuousGrid<Cell> grid, Vector3Int position)
        {
            Cell cellCopy = grid[position];

            cellCopy.cellState = CellState.Wall;

            grid[position] = cellCopy;
        }

        public static Vector3 ToWorld(this Vector3Int v)
        {
            return v + (Vector3.one / 2f);
        }

        public static Vector3Int FromWorld(this Vector3 v)
        {
            return Vector3Int.RoundToInt(v - (Vector3.one / 2f));
        }

        public static int MaskFlagCount(this int mask)
        {
            int count = 0;
            for (int i = 0; i < NEIGHBORS_ARRAY_SIZE; i++)
            {
                count += ((1 << i) & mask) >> i;
            }

            return count;
        }
    }
}