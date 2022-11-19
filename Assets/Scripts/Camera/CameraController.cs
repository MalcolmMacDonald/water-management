using UnityEngine;
using Utility;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Camera thisCamera;

    [SerializeField] private PipesGrid thisGrid;
    public float sphereCastRadius = 0.75f;

    [SerializeField] private PipeConnector startConnector;

    private bool isDragging;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Cursor.lockState = CursorLockMode.Confined;
        }

        HandleInput();
    }

    private void HandleInput()
    {
        Ray cameraRay = thisCamera.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;

            if (Physics.SphereCast(cameraRay, sphereCastRadius, out hit, float.MaxValue,
                    LayerMask.GetMask("Interactable")))
            {
                PipeConnector connector = hit.collider.GetComponentInParent<PipeConnector>();
                Debug.DrawRay(connector.transform.position, connector.transform.up, Color.red);
                if (startConnector == null)
                {
                    startConnector = connector;
                }

                SetInput(connector.position, connector.direction);
                return;
            }


            if (Physics.Raycast(cameraRay, out hit, float.MaxValue, ~LayerMask.GetMask("Interactable")))
            {
                Debug.DrawRay(hit.point, hit.normal);
                Vector3 cellPosition = hit.point + (hit.normal / 2f);
                if (thisGrid.PositionIsValid(cellPosition))
                {
                    SetInput(cellPosition.FromWorld(), Vector3Int.zero);
                }
            }
        }
        else
        {
            if (isDragging)
            {
                if (startConnector != null)
                {
                    RaycastHit hit;
                    PipeConnector connector = null;

                    if (Physics.SphereCast(cameraRay, sphereCastRadius, out hit, float.MaxValue,
                            LayerMask.GetMask("Interactable")))
                    {
                        connector = hit.collider.GetComponentInParent<PipeConnector>();
                    }

                    startConnector.Disconnect();
                    if (connector != null)
                    {
                        connector.Disconnect();
                    }

                    startConnector.Connect(connector);
                }

                startConnector = null;
                isDragging = false;
            }
        }
    }


    private void SetInput(Vector3Int position, Vector3Int direction)
    {
        if (!isDragging)
        {
            thisGrid.startPosition = position;
            thisGrid.startDirection = direction;
            isDragging = true;
            return;
        }

        thisGrid.endPosition = position;
        thisGrid.endDirection = direction;


        thisGrid.FindPath();
    }
}