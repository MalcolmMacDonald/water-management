using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Utility
{
    public static class Pathfinding
    {
        public static Vector3Int[] GetPath(this IGrid<Cell> grid, Vector3Int start, Vector3Int end)
        {
            Cell current = grid[end];
            var path = new List<Vector3Int>();
            foreach (Cell cell in grid.GetEnumerable())
            {
                Cell cellCopy = cell;
                cellCopy.pathCostSoFar = double.MaxValue;
                grid[cellCopy.position] = cellCopy;
            }

            var cameFrom = grid.GetFlowDictionary(start, end);

            while (current != grid[start])
            {
                path.Add(current.position);
                if (!cameFrom.ContainsKey(current))
                {
                    return Array.Empty<Vector3Int>();
                }


                if (cameFrom[current] == default)
                {
                    break;
                }

                current = cameFrom[current];
            }

            path.Add(grid[start].position);
            path.Reverse();
            return path.ToArray();
        }

        public static double Heuristic(Cell a, Cell b)
        {
            return //Mathf.Abs(a.position.x - b.position.x) + Mathf.Abs(a.position.y - b.position.y);
                Vector3Int.Distance(a.position, b.position);
            //Mathf.Max(Mathf.Abs(a.position.x - b.position.x), Mathf.Abs(a.position.y - b.position.y));
        }


        private static Dictionary<Cell, Cell> GetFlowDictionary(this IGrid<Cell> grid, Vector3Int start, Vector3Int end)
        {
            var frontier = new PriorityQueue<Cell>();

            frontier.Enqueue(grid[start], 0f);


            var cameFrom = new Dictionary<Cell, Cell>();
            cameFrom[grid[start]] = default;


            var cachedNeighborsList = new Cell[27 - 1];
            float[] cachedNeighborDistances = new float[27 - 1];
            while (frontier.Count > 0)
            {
                Cell currentCell = frontier.Dequeue();
                double currentCost = Math.Abs(currentCell.pathCostSoFar - double.MaxValue) < 2 ? 0
                    : currentCell.pathCostSoFar;

                currentCell.pathCostSoFar = currentCost;
                if (currentCell.position == end)
                {
                    break;
                }

                int neighborsCount = currentCell.neighborCount;
                grid.GetNeighborsNonAlloc(currentCell, ref cachedNeighborsList, ref cachedNeighborDistances);

                for (int i = 0; i < neighborsCount; i++)
                {
                    Cell neighbor = cachedNeighborsList[i];
                    if (neighbor.position == end)
                    {
                        cameFrom[neighbor] = currentCell;
                        return cameFrom;
                    }


                    double newCost = currentCost + cachedNeighborDistances[i];
                    newCost *= neighbor.cost;


                    if (newCost < neighbor.pathCostSoFar)
                    {
                        neighbor.pathCostSoFar = newCost;
                        grid[neighbor.position] = neighbor;

                        double priority = newCost + Heuristic(grid[end], neighbor);
                        frontier.Enqueue(neighbor, priority);
                        cameFrom[neighbor] = currentCell;
                    }
                }

                grid[currentCell.position] = currentCell;
            }


            return cameFrom;
        }
    }
}