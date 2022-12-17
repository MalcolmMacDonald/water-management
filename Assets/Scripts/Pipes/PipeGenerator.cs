using System;
using System.Collections.Generic;
using EasyButtons;
using Pipes;
using UnityEngine;
using Utility;
using static Utility.PipeUtilities;

public class PipeGenerator : MonoBehaviour
{
    [SerializeField] private PipesGrid gridController;
    [SerializeField] private MeshFilter thisFilter;
    [SerializeField] private MeshRenderer thisRenderer;
    public Transform startCap;
    public Transform endCap;
    private List<float> allDistances;
    private List<Vector3>[] allPipeNormals;
    private List<Vector3>[] allPipePoints;
    public PipeConfig pipeConfig;

    private bool shouldUpdateMesh;


    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePipe();
        }
    }

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
        }
    }

    public void GeneratePipe(Vector3Int[] positions)
    {
        thisRenderer.enabled = false;
        endCap.gameObject.SetActive(false);
        startCap.gameObject.SetActive(false);
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
        if (gridController.startDirection == Vector3Int.zero)
        {
            startCap.gameObject.SetActive(true);
            startCap.position = simplifiedPath[0].ToWorld();
            //startCap.rotation = currentRotation;
        }
        if (gridController.endDirection == Vector3Int.zero)
        {
            endCap.gameObject.SetActive(true);
            endCap.position = simplifiedPath[^1].ToWorld();
            //endCap.rotation = currentRotation;
        }

        thisFilter.mesh = PipeMeshBuilder.CreatePipe(pipeConfig, simplifiedPath);
    }
}