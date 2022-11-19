using System;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;
using Utility;

[ExecuteAlways]
[SelectionBase]
public class Tank : MonoBehaviour
{
    public Vector3Int size;
    [SerializeField] private GameObject displayObject;
    [SerializeField] private Transform waterObject;

    [SerializeField] [Range(0f, 1f)] private float fillLevel;

    [SerializeField] private float waterEdgeOffset = 0.05f;

    [SerializeField] private List<TankConnector> tankConnectors;
    [SerializeField] private Vector3Int position;
    [SerializeField] private Transform planesContainer;
    [SerializeField] private Transform[] particleCollisionPlanes;

    [SerializeField] private float visualFillLevelLerpSpeed;
    private float visualFillLevel;

    private void Update()
    {
        if (!Application.isPlaying)
        {
            visualFillLevel = fillLevel;
            OnValidate();
        }
        else
        {
            FlowWater();
        }

        visualFillLevel = Damp(visualFillLevel, fillLevel, visualFillLevelLerpSpeed, Time.deltaTime);


        Vector3 waterSize = size;

        waterSize.y = size.y * (visualFillLevel - waterEdgeOffset);
        waterSize.y = Mathf.Max(waterSize.y, 0);

        waterObject.localPosition = waterSize / 2;

        waterSize -= Vector3.one * waterEdgeOffset;
        waterSize.y = Mathf.Max(waterSize.y, 0);
        waterObject.localScale = waterSize;
        waterObject.gameObject.SetActive(waterSize.y > Mathf.Epsilon);
        SetWaterPlanes();
    }


    private void OnEnable()
    {
        visualFillLevel = fillLevel;
        GetPipeData();
    }

    private void OnValidate()
    {
        displayObject.transform.localPosition = (Vector3)size / 2f;
        displayObject.transform.localScale = size;
        GetPipeData();
        SetupParticleCollisionPlanes();
    }

    public static float Damp(float a, float b, float lambda, float dt)
    {
        return Mathf.Lerp(a, b, 1 - Mathf.Exp(-lambda * dt));
    }

    private float AddWater(PipeConnector connector, float amount)
    {
        TankConnector thisConnector = tankConnectors.Find(tankConnector => tankConnector.connector == connector);
        if (!thisConnector.pumpDirection.HasFlag(PumpDirection.In))
        {
            return 0;
        }

        float maxConnectorAmount = thisConnector.minimumFillLevel;
        float fillLevelOffset = Mathf.Clamp(fillLevel + amount, 0, maxConnectorAmount) - fillLevel;
        fillLevelOffset = Mathf.Clamp01(fillLevelOffset);

        fillLevel += fillLevelOffset;
        return fillLevelOffset;
    }

    private void GetPipeData()
    {
        var connectors = GetComponentsInChildren<PipeConnector>();


        tankConnectors = new List<TankConnector>();
        position = transform.position.FromWorld();

        foreach (PipeConnector pipeConnector in connectors)
        {
            Vector3Int connectorDirection = pipeConnector.direction;
            PumpDirection pumpDirection = 0;
            if (connectorDirection.y <= 0)
            {
                pumpDirection |= PumpDirection.Out;
            }

            if (connectorDirection.y >= 0)
            {
                pumpDirection |= PumpDirection.In;
            }

            float normalizedFillLevel = Mathf.Clamp01(
                ((pipeConnector.position - position).y + 1) /
                (float)size.y);
            pipeConnector.onEnter = AddWater;
            pipeConnector.SetParticleCollisionPlanes(particleCollisionPlanes);
            tankConnectors.Add(new TankConnector
            {
                connector = pipeConnector,
                pumpDirection = pumpDirection,
                minimumFillLevel = normalizedFillLevel
            });
        }
    }

    [Button]
    public void Fill()
    {
        fillLevel = 1;
        visualFillLevel = fillLevel;
    }

    private void FlowWater()
    {
        var flowingConnectors = new List<TankConnector>();
        foreach (TankConnector tankConnector in tankConnectors)
        {
            if (!tankConnector.pumpDirection.HasFlag(PumpDirection.Out))
            {
                continue;
            }

            if (!tankConnector.connector.isActiveAndEnabled)
            {
                continue;
            }

            if (fillLevel > tankConnector.minimumFillLevel)
            {
                flowingConnectors.Add(tankConnector);
            }
        }


        float waterFlownOut = 0;
        foreach (TankConnector tankConnector in flowingConnectors)
            //flow out here
            waterFlownOut += tankConnector.connector.FlowWaterOut(fillLevel);

        fillLevel -= waterFlownOut;
    }

    private void SetupParticleCollisionPlanes()
    {
        Vector3[] planePositions =
        {
            new Vector3(0, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 1f),
            new Vector3(1f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, 1f, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        Vector3[] planeNormals =
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, -1, 0),
            new Vector3(0, 1, 0)
        };

        if (transform.childCount < 8)
        {
            for (int i = 0; i < 8; i++)
            {
                int childCount = planesContainer.childCount;
                if (childCount < i + 1)
                {
                    GameObject newPlane = new GameObject("Plane " + i);
                    newPlane.transform.parent = planesContainer;
                }
            }
        }

        particleCollisionPlanes = new Transform[8];
        for (int i = 0; i < 6; i++)
        {
            Transform thisPlane = planesContainer.GetChild(i);
            thisPlane.localPosition = Vector3.Scale(planePositions[i], size);
            thisPlane.up = planesContainer.TransformDirection(planeNormals[i]);
            particleCollisionPlanes[i] = thisPlane;
        }

        SetWaterPlanes();
    }

    private void SetWaterPlanes()
    {
        particleCollisionPlanes[6] = planesContainer.GetChild(6);
        particleCollisionPlanes[6].localPosition = Vector3.Scale(new Vector3(0.5f, visualFillLevel, 0.5f), size);
        particleCollisionPlanes[6].up = Vector3.up;
        particleCollisionPlanes[7] = planesContainer.GetChild(7);
        particleCollisionPlanes[7].localPosition = Vector3.Scale(new Vector3(0.5f, visualFillLevel, 0.5f), size);
        particleCollisionPlanes[7].up = Vector3.down;
        foreach (TankConnector tankConnector in tankConnectors)
            tankConnector.connector.SetParticleCollisionPlanes(particleCollisionPlanes);
    }

    [Serializable]
    private class TankConnector
    {
        public PipeConnector connector;
        public float minimumFillLevel;
        public PumpDirection pumpDirection = 0;
    }
}

[Flags]
public enum PumpDirection
{
    Empty = 0,
    In = 1 << 0,
    Out = 1 << 1
}