using System;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;
using Utility;
using static Utility.PipeUtilities;

public class PipeGenerator : MonoBehaviour
{
    [Flags]
    public enum SegmentState
    {
        None = 1 << 0,
        Start = 1 << 1,
        MiteredStart = 1 << 2,
        End = 1 << 3,
        MiteredEnd = 1 << 4
    }

    private static readonly int PIPE_POLYGON_SIDES = 12;

    [SerializeField] private PipesGrid gridController;
    public float pipeRadius = 0.75f;
    public float miterDistance = 0.3f;
    [SerializeField] private MeshFilter thisFilter;
    [SerializeField] private MeshRenderer thisRenderer;
    public Transform startCap;
    public Transform endCap;
    private List<float> allDistances;
    private List<Vector3>[] allPipeNormals;
    private List<Vector3>[] allPipePoints;


    private bool shouldUpdateMesh;


    private void Start()
    {
        gridController.onPathUpdated?.AddListener(QueueMeshUpdate);
        thisFilter.mesh.MarkDynamic();
    }

    private void LateUpdate()
    {
        if (shouldUpdateMesh)
        {
            UpdatePipe();
            shouldUpdateMesh = false;
        }
    }

    private void OnDestroy()
    {
        gridController.onPathUpdated?.RemoveListener(QueueMeshUpdate);
    }

    private void QueueMeshUpdate()
    {
        shouldUpdateMesh = true;
    }

    [Button]
    private void UpdatePipe()
    {
        if (gridController.foundPath.Length > 0)
        {
            GeneratePipe(gridController.foundPath);
            CreateMesh(allPipePoints, allPipeNormals, allDistances);
        }
    }

    public void GeneratePipe(Vector3Int[] positions)
    {
        thisRenderer.enabled = false;
        allPipePoints = new List<Vector3>[PIPE_POLYGON_SIDES];
        allPipeNormals = new List<Vector3>[PIPE_POLYGON_SIDES];
        endCap.gameObject.SetActive(false);
        startCap.gameObject.SetActive(false);

        for (int j = 0; j < PIPE_POLYGON_SIDES; j++)
        {
            allPipePoints[j] = new List<Vector3>();
            allPipeNormals[j] = new List<Vector3>();
        }

        if (positions.Length == 0)
        {
            return;
        }

        var simplifiedPath = SimplifyPath(gridController.foundPath);
        if (simplifiedPath.Count < 2)
        {
            return;
        }

        thisRenderer.enabled = true;
        Vector3 startDirection = simplifiedPath[0].ToWorld() - simplifiedPath[1].ToWorld();
        Vector3 lastDirection = startDirection;
        Quaternion currentRotation =
            Quaternion.LookRotation(startDirection, Vector3.up);
        allDistances = new List<float>();
        if (Math.Abs(Mathf.Abs(startDirection.normalized.y) - 1) < 0.0001f)
        {
            currentRotation = Quaternion.LookRotation(startDirection, Vector3.forward);
        }

        if (gridController.startDirection == Vector3Int.zero)
        {
            startCap.gameObject.SetActive(true);
            startCap.position = simplifiedPath[0].ToWorld();
            startCap.rotation = currentRotation;
        }


        for (int i = 0; i < simplifiedPath.Count - 1; i++)
        {
            Vector3 thisPosition = simplifiedPath[i].ToWorld();
            Vector3 nextPosition = simplifiedPath[i + 1].ToWorld();
            Vector3 thisDirection = (nextPosition - thisPosition).normalized;
            Vector3 nextDirection = (nextPosition - thisPosition).normalized;

            if (i < simplifiedPath.Count - 2)
            {
                nextDirection = (simplifiedPath[i + 2].ToWorld() - simplifiedPath[i + 1].ToWorld()).normalized;
            }

            SegmentState currentState = SegmentState.None;
            if (i == 0)
            {
                currentState |= SegmentState.Start;
            }
            else
            {
                currentState |= SegmentState.MiteredStart;
            }

            if (i == simplifiedPath.Count - 2)
            {
                currentState |= SegmentState.End;
            }
            else
            {
                currentState |= SegmentState.MiteredEnd;
            }

            currentRotation = Quaternion.FromToRotation(lastDirection, thisDirection) * currentRotation;
            AddSegmentPoints(ref allPipePoints, ref allPipeNormals, ref allDistances, thisPosition, nextPosition,
                currentRotation, currentState);
            lastDirection = (nextPosition - thisPosition).normalized;
        }

        if (gridController.endDirection == Vector3Int.zero)
        {
            endCap.gameObject.SetActive(true);
            endCap.position = simplifiedPath[^1].ToWorld();
            endCap.rotation = currentRotation;
        }
    }

    private void CreateMesh(List<Vector3>[] allPipePoints, List<Vector3>[] allPipeNormals, List<float> distances)
    {
        int crossSegmentCount = allPipePoints[0].Count;
        Mesh newMesh = thisFilter.mesh;
        var triangles = new List<int>();

        var reorderedVertices = new List<Vector3>();
        var reorderedNormals = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int i = 0; i < crossSegmentCount; i++)
        {
            for (int j = 0; j < PIPE_POLYGON_SIDES; j++)
            {
                reorderedVertices.Add(allPipePoints[j][i]);
                reorderedNormals.Add(allPipeNormals[j][i]);
                float thisY = j / 8f;


                float thisX = distances[i];
                uvs.Add(new Vector2(thisX, thisY));

                if (i == crossSegmentCount - 1)
                {
                    continue;
                }

                int nextI = i + 1;
                int nextJ = j + 1;
                triangles.Add(GetVertexIndex(i, j));
                triangles.Add(GetVertexIndex(i, nextJ));
                triangles.Add(GetVertexIndex(nextI, nextJ));

                triangles.Add(GetVertexIndex(i, j));
                triangles.Add(GetVertexIndex(nextI, nextJ));
                triangles.Add(GetVertexIndex(nextI, j));
            }

            uvs.Add(new Vector2(distances[i], 1f));

            reorderedVertices.Add(allPipePoints[0][i]);
            reorderedNormals.Add(allPipeNormals[0][i]);
        }

        var vertices = reorderedVertices.ToArray();
        var normals = reorderedNormals.ToArray();
        int[] trianglesArray = triangles.ToArray();
        newMesh.Clear();
        newMesh.SetVertices(vertices);
        newMesh.RecalculateBounds();
        newMesh.SetIndices(trianglesArray, MeshTopology.Triangles, 0);
        //newMesh.triangles = trianglesArray;
        newMesh.uv = uvs.ToArray();
        newMesh.normals = normals;

        newMesh.MarkModified();
        //thisFilter.mesh = newMesh;
    }

    private int GetVertexIndex(int i, int j)
    {
        return (i * (PIPE_POLYGON_SIDES + 1)) + j;
    }


    private void AddSegmentPoints(ref List<Vector3>[] allPipePoints, ref List<Vector3>[] allPipeNormals,
        ref List<float> distances, Vector3 start, Vector3 end,
        Quaternion currentRotation, SegmentState state)
    {
        if (state.HasFlag(SegmentState.Start))
        {
            if (distances.Count == 0)
            {
                distances.Add(0f);
            }
            else
            {
                distances.Add(distances[^1]);
            }

            AddCrossSection(ref allPipePoints, ref allPipeNormals, start, currentRotation);
        }

        if (state.HasFlag(SegmentState.MiteredStart))
        {
            distances.Add(distances[^1] + (miterDistance * 2));
            AddCrossSection(ref allPipePoints, ref allPipeNormals, GetMiteredPoint(start, end, miterDistance),
                currentRotation);
        }

        if (state.HasFlag(SegmentState.MiteredEnd))
        {
            distances.Add(distances[^1] + (Vector3.Distance(start, end) - miterDistance));
            AddCrossSection(ref allPipePoints, ref allPipeNormals, GetMiteredPoint(end, start, miterDistance),
                currentRotation);
        }


        if (state.HasFlag(SegmentState.End))
        {
            distances.Add(distances[^1] + Vector3.Distance(start, end));
            AddCrossSection(ref allPipePoints, ref allPipeNormals, end, currentRotation);
        }
    }

    private void AddCrossSection(ref List<Vector3>[] allPipePoints, ref List<Vector3>[] allPipeNormals,
        Vector3 position,
        Quaternion rotation)
    {
        var crossSectionPoints = GetPipeCrossSection(position, rotation, PIPE_POLYGON_SIDES, pipeRadius);
        for (int j = 0; j < PIPE_POLYGON_SIDES; j++)
        {
            allPipePoints[j].Add(crossSectionPoints[j]);
            allPipeNormals[j].Add((crossSectionPoints[j] - position).normalized);
        }
    }
}