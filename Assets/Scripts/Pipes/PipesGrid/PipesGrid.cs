using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using EasyButtons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Utility;

[Flags]
public enum DebugDisplay
{
    None = 0,
    Walls = 1 << 0,
    Neighbors = 1 << 1,
    Costs = 1 << 2
}

[ExecuteAlways]
public class PipesGrid : MonoBehaviour
{
    public Vector3Int[] foundPath;

    [SerializeField] private HashedGrid<Cell> hashedGrid;

    public int gridWidth;
    public int gridHeight;
    public int gridDepth;
    public Vector3Int startPosition;
    public Vector3Int startDirection;
    public Vector3Int endPosition;
    public Vector3Int endDirection;
    public DebugDisplay displayConfig;
    public UnityEvent onPathUpdated;

    private BoundsInt internalBounds;

    public HashedGrid<Cell> HashedGrid
    {
        get
        {
            if (!hashedGrid.Initialized())
            {
                Init();
            }

            return hashedGrid;
        }
        set => hashedGrid = value;
    }

    private void Awake()
    {
        onPathUpdated = new UnityEvent();
    }


    private void Update()
    {
        for (int i = 0; i < foundPath.Length - 1; i++)
        {
            Debug.DrawLine(foundPath[i].ToWorld(), foundPath[i + 1].ToWorld(), Color.blue);
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (displayConfig == DebugDisplay.None)
        {
            return;
        }

        foreach (Cell cell in hashedGrid.GetEnumerable())
        {
            if (cell.cellState == CellState.Wall && displayConfig.HasFlag(DebugDisplay.Walls))
            {
                Gizmos.DrawWireCube(cell.position.ToWorld(), Vector3.one);
            }

            if (displayConfig.HasFlag(DebugDisplay.Neighbors))
            {
                foreach (Cell neighbor in hashedGrid.GetNeighbors(cell))
                    Gizmos.DrawRay(cell.position.ToWorld(),
                        (neighbor.position.ToWorld() - cell.position.ToWorld()).normalized / 3f);

                Handles.Label(cell.position.ToWorld(), cell.neighborCount.ToString());
            }

            if (displayConfig.HasFlag(DebugDisplay.Costs))
            {
                Handles.Label(cell.position.ToWorld(), cell.cost.ToString());
            }
        }
    }
#endif

    private void OnValidate()
    {
        if (!HashedGrid.Initialized())
        {
            Init();
        }
    }

    private void Init()
    {
        hashedGrid.OnAfterDeserialize();
    }

    [Button]
    public void FindPath()
    {
        var newPath = hashedGrid.GetPath(startPosition + (startDirection * 2),
            endPosition + (endDirection * 2));
        if (newPath.Length == 0)
        {
            return;
        }

        foundPath = newPath;
        if (startDirection != Vector3Int.zero)
        {
            foundPath = foundPath.Prepend(startPosition + startDirection).Prepend(startPosition).ToArray();
        }

        if (endDirection != Vector3Int.zero)
        {
            foundPath = foundPath.Append(endPosition + endDirection).Append(endPosition).ToArray();
        }

        onPathUpdated?.Invoke();
    }

    public void GenerateGrid()
    {
        hashedGrid = new HashedGrid<Cell>();
        internalBounds = new BoundsInt(1, 1, 1, gridWidth - 2, gridHeight - 2, gridDepth - 2);

        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                for (int k = 0; k < gridDepth; k++)
                {
                    Vector3Int intPosition = new Vector3Int(i, j, k);
                    Vector3 position = intPosition.ToWorld();

                    CellState state = CellState.Empty;
                    double cost = 1;

                    bool isWall = Physics.CheckBox(position, Vector3.one / 4f, Quaternion.identity,
                        ~LayerMask.GetMask("PipeAllowance"));
                    if (isWall)
                    {
                        continue;
                    }

                    hashedGrid[i, j, k] = new Cell
                    {
                        position = intPosition, cellState = state, cost = cost, pathCostSoFar = double.MaxValue
                    };
                }
            }
        }

        hashedGrid.UpdateNeighbors();

        foreach (Cell cell in hashedGrid.GetEnumerable())
        {
            int neighborCount = cell.neighborCount;
            if (Physics.CheckBox(cell.position.ToWorld(), Vector3.one / 4f, Quaternion.identity,
                    LayerMask.GetMask("PipeAllowance")))
            {
                continue;
            }

            if (neighborCount == 26)
            {
                hashedGrid.Remove(cell.position);
                continue;
            }

            if (IsOnBoundary(cell.position))
            {
                hashedGrid.Remove(cell.position);
            }
        }

        hashedGrid.UpdateNeighbors();
        hashedGrid.UpdateCosts();
        OnValidate();
    }

    private bool IsOnBoundary(Vector3Int position)
    {
        return !internalBounds.Contains(position);
    }

    public bool PositionIsValid(Vector3 position)
    {
        return hashedGrid.HasValue(position.FromWorld());
    }

    [Button]
    private void GenerateCollisionGrid()
    {
        GenerateGrid();
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        EditorSceneManager.sceneSaving += GenerateGridOnSave;
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneSaving -= GenerateGridOnSave;
    }

    private static List<Transform> AllParents(Transform transform)
    {
        var parents = new List<Transform> { transform };
        Transform currentTransform = transform;
        while (currentTransform.transform.parent != null)
        {
            Transform parent = currentTransform.transform.parent;
            parents.Add(parent);
            currentTransform = parent;
        }

        return parents;
    }

    private void GenerateGridOnSave(Scene scene, string path)
    {
        if (!EditorPrefs.GetBool("GenerateCollisionOnSave", false))
        {
            return;
        }

        var allColliders = FindObjectsOfType<Collider>(true);

        bool anyDirtyColliders = allColliders.Any(collider =>
            EditorUtility.IsDirty(collider) ||
            AllParents(collider.transform).Any(transform =>
                EditorUtility.IsDirty(transform) || EditorUtility.IsDirty(transform.gameObject)));

        if (anyDirtyColliders)
        {
            GenerateCollisionGrid();
        }
    }

#endif
}