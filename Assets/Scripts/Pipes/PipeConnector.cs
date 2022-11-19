using UnityEngine;
using Utility;

[ExecuteAlways]
[SelectionBase]
public class PipeConnector : MonoBehaviour
{
    public delegate float WaterEnterEvent(PipeConnector connector, float amount);

    public delegate float WaterExitEvent(PipeConnector connector, float amount);

    public PipesGrid thisGrid;
    public Vector3Int position;
    public Vector3Int direction;

    public PipeConnector otherConnector;
    [SerializeField] private ParticleSystem waterFlowingInParticles;
    [SerializeField] private ParticleSystem waterFlowingOutParticles;

    public float flowSpeed = 0.5f;
    public PumpDirection pumpDirection;

    public WaterEnterEvent onEnter;
    public WaterExitEvent onExit;


    private void Update()
    {
        if (!Application.isPlaying)
        {
            OnValidate();
        }

        if (pumpDirection.HasFlag(PumpDirection.Out))
        {
            if (otherConnector != null)
            {
                otherConnector.FlowWaterIn(flowSpeed * Time.deltaTime);
            }
        }

        if (pumpDirection.HasFlag(PumpDirection.In))
        {
            if (otherConnector != null)
            {
                otherConnector.FlowWaterOut(flowSpeed * Time.deltaTime);
            }
        }
    }

    private void OnEnable()
    {
        if (!GetComponentInParent<Tank>())
        {
            if (pumpDirection.HasFlag(PumpDirection.In))
            {
                onEnter = (connector, amount) => Mathf.Min(amount, flowSpeed * Time.deltaTime);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (otherConnector != null)
        {
            Debug.DrawLine(transform.position, otherConnector.transform.position, Color.red);
        }
    }

    public void OnValidate()
    {
        thisGrid = GetComponentInParent<PipesGrid>();
        position = transform.position.FromWorld();
        direction = Vector3Int.RoundToInt(transform.up);
    }

    public void SetParticleCollisionPlanes(Transform[] planes)
    {
        ParticleSystem.CollisionModule collisionModule = waterFlowingInParticles.collision;
        int planeCounter = 0;
        for (int i = 0; i < planes.Length; i++)
        {
            if (i == planes.Length - 1)
            {
                continue;
            }

            collisionModule.SetPlane(planeCounter, planes[i]);
            planeCounter++;
        }

        planeCounter = 0;
        collisionModule = waterFlowingOutParticles.collision;
        for (int i = 0; i < planes.Length; i++)
        {
            if (i == planes.Length - 2)
            {
                continue;
            }

            collisionModule.SetPlane(planeCounter, planes[i]);
            planeCounter++;
        }
    }


    public float FlowWaterOut(float amount)
    {
        float outAmount = 0;

        amount = Mathf.Min(amount, flowSpeed * Time.deltaTime);
        if (otherConnector != null)
        {
            amount = Mathf.Min(amount, otherConnector.flowSpeed * Time.deltaTime);
            outAmount += otherConnector.FlowWaterIn(amount);
        }

        if (amount > 0 && outAmount > 0)
        {
            if (!waterFlowingOutParticles.isEmitting && pumpDirection == PumpDirection.Empty)
            {
                waterFlowingOutParticles.Play();
            }
        }

        return outAmount;
    }

    public float FlowWaterIn(float amount)
    {
        float inAmount = 0;
        amount = Mathf.Min(amount, flowSpeed * Time.deltaTime);
        if (onEnter != null)
        {
            inAmount += onEnter.Invoke(this, amount);
        }

        if (inAmount > 0)
        {
            if (!waterFlowingInParticles.isEmitting && pumpDirection == PumpDirection.Empty)
            {
                waterFlowingInParticles.Play();
            }
        }

        return inAmount;
    }

    public void Disconnect()
    {
        PipeConnector olOtherConnector = otherConnector;
        otherConnector = null;
        if (olOtherConnector != null)
        {
            olOtherConnector.Disconnect();
        }
    }

    public void Connect(PipeConnector connector)
    {
        if (connector == otherConnector)
        {
            return;
        }

        otherConnector = connector;
        otherConnector.Connect(this);
    }
}