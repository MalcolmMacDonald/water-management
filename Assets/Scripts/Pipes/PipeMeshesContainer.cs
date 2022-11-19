using System.Collections.Generic;
using UnityEngine;

public class PipeMeshesContainer : MonoBehaviour
{
    public GameObject pipeMeshPrefab;

    public Dictionary<PipeConnector, Transform> connectorMeshesDictionary = new Dictionary<PipeConnector, Transform>();

    public void CreateMesh(PipeConnector connector, Mesh mesh)
    {
        GameObject newMeshObject = Instantiate(pipeMeshPrefab, transform.position, Quaternion.identity, transform);
        MeshFilter newMeshFilter = newMeshObject.GetComponent<MeshFilter>();
        newMeshFilter.mesh = mesh;
        connectorMeshesDictionary.Add(connector, newMeshObject.transform);
        connectorMeshesDictionary.Add(connector.otherConnector, newMeshObject.transform);
    }

    public void RemoveMesh(PipeConnector connector)
    {
        Destroy(connectorMeshesDictionary[connector].gameObject);
        connectorMeshesDictionary.Remove(connector);
        connectorMeshesDictionary.Remove(connector.otherConnector);
    }
}