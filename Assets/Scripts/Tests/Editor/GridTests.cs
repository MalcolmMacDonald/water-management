using System.Collections.Generic;
using Data;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine;
using Utility;

public class GridTests
{
    private static int[] gridSize = { 80 };
    private static float[] pValue = { 0.001f };

    private static readonly SampleGroup[] hashedGridSampleGroups =
    {
        new SampleGroup("Creation.Initial"),
        new SampleGroup("Creation.Culling"),
        new SampleGroup("Pathfinding"),
        new SampleGroup("Cell Count"),
        new SampleGroup("Creation.UpdateNeighbors")
    };


    private static readonly SampleGroup[] continuousSampleGroups =
    {
        new SampleGroup("Creation"),
        new SampleGroup("Pathfinding"),
        new SampleGroup("Cell Count")
    };

    [Test]
    [Performance]
    public void HashedGrid([ValueSource("gridSize")] int gridSize, [ValueSource("pValue")] float pValue)
    {
        Measure.Method(() =>
        {
            using (Measure.ProfilerMarkers(hashedGridSampleGroups))
            {
                ProfilerMarker pathfindingMarker = new ProfilerMarker(hashedGridSampleGroups[2].Name);


                var grid = new HashedGrid<Cell>();
                SetupGrid(grid, gridSize, pValue, hashedGridSampleGroups);

                CullGrid(grid, hashedGridSampleGroups);


                pathfindingMarker.Begin();
                grid.GetPath(Vector3Int.zero, new Vector3Int(gridSize, gridSize, gridSize) - Vector3Int.one);

                pathfindingMarker.End();

                Measure.Custom(hashedGridSampleGroups[3], grid.elementCount);
            }
        }).GC().Run();
    }

    private void SetupGrid(HashedGrid<Cell> grid, int gridSize, float pValue, SampleGroup[] sampleGroups)
    {
        ProfilerMarker initialCreationMarker = new ProfilerMarker(sampleGroups[0].Name);

        initialCreationMarker.Begin();

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int k = 0; k < gridSize; k++)
                {
                    Vector3Int intPosition = new Vector3Int(i, j, k);
                    CellState state = CellState.Empty;
                    double cost = 0;

                    bool isWall = Random.Range(0, 1f) < pValue;
                    if (isWall)
                    {
                        continue;
                    }

                    grid[i, j, k] = new Cell
                    {
                        position = intPosition, cellState = state, cost = cost,
                        pathCostSoFar = double.MaxValue
                    };
                }
            }
        }

        initialCreationMarker.End();
    }

    private void CullGrid(HashedGrid<Cell> grid, SampleGroup[] sampleGroups)
    {
        ProfilerMarker cullingCreationMarker = new ProfilerMarker(sampleGroups[1].Name);
        ProfilerMarker updateNeighborsMarker = new ProfilerMarker(sampleGroups[4].Name);

        cullingCreationMarker.Begin();

        updateNeighborsMarker.Begin();
        grid.UpdateNeighbors();
        updateNeighborsMarker.End();

        var cellsToRemove = new List<Cell>();
        foreach (Cell cell in grid.GetEnumerable())
        {
            int neighborCount = cell.neighborCount;

            if (neighborCount == 26)
            {
                cellsToRemove.Add(cell);
            }
        }

        foreach (Cell cell in cellsToRemove) grid.Remove(cell.position);

        grid.TrimExcess();

        updateNeighborsMarker.Begin();
        grid.UpdateNeighbors();
        updateNeighborsMarker.End();
        cullingCreationMarker.End();
    }


    [Test]
    [Performance]
    public void ContinuousGrid([ValueSource("gridSize")] int gridSize, [ValueSource("pValue")] float pValue)
    {
        Measure.Method(() =>
        {
            using (Measure.ProfilerMarkers(continuousSampleGroups))
            {
                ProfilerMarker creationMarker = new ProfilerMarker(continuousSampleGroups[0].Name);
                ProfilerMarker pathfindingMarker = new ProfilerMarker(continuousSampleGroups[1].Name);

                creationMarker.Begin();
                var grid = new ContinuousGrid<Cell>(gridSize, gridSize, gridSize);
                for (int i = 0; i < gridSize; i++)
                {
                    for (int j = 0; j < gridSize; j++)
                    {
                        for (int k = 0; k < gridSize; k++)
                        {
                            Vector3Int intPosition = new Vector3Int(i, j, k);
                            CellState state = CellState.Empty;
                            double cost = 0;

                            bool isWall = Random.Range(0, 1f) < pValue;
                            if (isWall)
                            {
                                state = CellState.Wall;
                                cost = 1;
                            }

                            grid[i, j, k] = new Cell
                            {
                                position = intPosition, cellState = state, cost = cost,
                                pathCostSoFar = double.MaxValue
                            };
                        }
                    }
                }

                grid.UpdateNeighbors();

                foreach (Cell cell in grid.GetEnumerable())
                {
                    int neighborCount = cell.neighborCount;

                    if (neighborCount == 26)
                    {
                        grid.Remove(cell.position);
                    }
                }

                grid.UpdateNeighbors();

                creationMarker.End();

                pathfindingMarker.Begin();
                grid.GetPath(Vector3Int.zero, new Vector3Int(gridSize, gridSize, gridSize) - Vector3Int.one);
                pathfindingMarker.End();
                Measure.Custom(continuousSampleGroups[2], grid.flatGrid.Length);
            }
        }).GC().Run();
    }
}