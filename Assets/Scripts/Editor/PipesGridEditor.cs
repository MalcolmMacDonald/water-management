using System.Collections.Generic;
using System.Linq;
using Data;
using EasyButtons.Editor;
using UnityEditor;
using UnityEngine;
using Utility;

namespace Editor
{
    //  [CustomEditor(typeof(PipesGrid))]
    public class PipesGridEditor : UnityEditor.Editor
    {
        private ButtonsDrawer _buttonsDrawer;

        private bool hasStart;


        private void OnEnable()
        {
            _buttonsDrawer = new ButtonsDrawer(target);
        }


        private void OnSceneGUI()
        {
            var pipesGrid = ((PipesGrid)target).HashedGrid;
            var allClickedCells = new List<Cell>();
            foreach (Cell cell in pipesGrid.GetEnumerable())
            {
                if (cell.cellState != CellState.Wall)
                {
                    continue;
                }

                Vector3 position = cell.position.ToWorld();
                Vector3 size = new Vector3(1, 1, 1) * 0.8f;

                Quaternion rotation = Quaternion.AngleAxis(90, Vector3.right);
                CellState currentState = cell.cellState;
                Color cellColor = currentState == CellState.Empty ? new Color(0f, 0f, 0f, 0f) : Color.black;

                if (CellButton(position, rotation, size, cellColor))
                {
                    allClickedCells.Add(cell);
                }
            }

            if (allClickedCells.Count == 0)
            {
                return;
            }

            Vector3 cameraPosition = SceneView.currentDrawingSceneView.camera.transform.position;
            Vector3 cameraDirection = SceneView.currentDrawingSceneView.camera.transform.forward;

            float CameraDistance(Vector3 camPosition, Vector3 direction, Vector3 targetPosition)
            {
                float distance = Vector3.Dot((targetPosition - camPosition).normalized, direction);
                if (distance < 0)
                {
                    distance = float.MaxValue;
                }

                return distance;
            }

            /*
        var nearestClickedCell = allClickedCells
            .OrderBy(cell => CameraDistance(cameraPosition, cameraDirection, cell.position.ToWorld())).First();
            */
            var validCells =
                allClickedCells.SelectMany(nearestClickedCell => pipesGrid.GetNeighbors(nearestClickedCell));
            if (!validCells.Any())
            {
                return;
            }

            Cell nearestCell = validCells
                .OrderBy(cell => CameraDistance(cameraPosition, cameraDirection, cell.position.ToWorld())).First();


            if (!hasStart)
            {
                ((PipesGrid)target).startPosition = nearestCell.position;
                hasStart = true;
            }
            else
            {
                ((PipesGrid)target).endPosition = nearestCell.position;
            }

            ((PipesGrid)target).FindPath();

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _buttonsDrawer.DrawButtons(targets);
        }

        private bool CellButton(Vector3 position, Quaternion rotation, Vector3 size, Color color)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive); // Gets a new ControlID for the handle

            bool buttonOutput = false;


            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.Layout:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCube(position, rotation, size.x));
                    break;
                case EventType.MouseUp:
                    /* if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                 {
                     buttonOutput = true;
                     Event.current.Use();
                 }*/

                    hasStart = false;
                    break;
                case EventType.MouseDrag:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCube(position, rotation, size.x));

                    if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                    {
                        buttonOutput = true;

                        Event.current.Use();
                    }

                    break;
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCube(position, rotation, size.x));

                    break;

                case EventType.Repaint:
                    if (HandleUtility.nearestControl == controlID)
                    {
                        Handles.color = Color.red;
                    }
                    else
                    {
                        Handles.color = color;
                    }

                    break;
            }

            return buttonOutput;
        }
    }
}