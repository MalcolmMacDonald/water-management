using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

namespace Pipes
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
    public static class PipeMeshBuilder
    {
        public static Mesh CreatePipe(PipeConfig pipeConfig, List<Vector3Int> simplifiedPath)
        {
            var allPipePoints = new List<Vector3>[pipeConfig.sides];
            var allPipeNormals = new List<Vector3>[pipeConfig.sides];
            for (int j = 0; j < pipeConfig.sides; j++)
            {
                allPipePoints[j] = new List<Vector3>();
                allPipeNormals[j] = new List<Vector3>();
            }
            
            Vector3 startDirection = simplifiedPath[0].ToWorld() - simplifiedPath[1].ToWorld();
            Vector3 lastDirection = startDirection;
            Quaternion currentRotation = Quaternion.LookRotation(startDirection, Vector3.up);
            var  allDistances = new List<float>();
            if (Math.Abs(Mathf.Abs(startDirection.normalized.y) - 1) < 0.0001f)
            {
                currentRotation = Quaternion.LookRotation(startDirection, Vector3.forward);
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
                

                Quaternion newRotation = Quaternion.FromToRotation(lastDirection, thisDirection) * currentRotation;
                AddSegmentPoints(pipeConfig, ref allPipePoints, ref allPipeNormals, ref allDistances, thisPosition,
                    nextPosition,
                    currentRotation, newRotation, currentState);
                currentRotation = newRotation;
                lastDirection = (nextPosition - thisPosition).normalized;
            }
            
            return CreateMesh(pipeConfig, allPipePoints, allPipeNormals, allDistances);
        }
        
        
        private static Mesh CreateMesh(PipeConfig config,List<Vector3>[] allPipePoints, List<Vector3>[] allPipeNormals,
            List<float> distances)
        {
            int crossSegmentCount = allPipePoints[0].Count;
            Mesh newMesh = new Mesh();
            var triangles = new List<int>();

            var reorderedVertices = new List<Vector3>();
            var reorderedNormals = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int i = 0; i < crossSegmentCount; i++)
            {
                for (int j = 0; j < config.sides; j++)
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
                    triangles.Add(GetVertexIndex(config, i, j));
                    triangles.Add(GetVertexIndex(config, i, nextJ));
                    triangles.Add(GetVertexIndex(config, nextI, nextJ));

                    triangles.Add(GetVertexIndex(config, i, j));
                    triangles.Add(GetVertexIndex(config, nextI, nextJ));
                    triangles.Add(GetVertexIndex(config, nextI, j));
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
            newMesh.uv = uvs.ToArray();
            newMesh.normals = normals;
            newMesh.MarkModified();
            return newMesh;
        }



        private static void AddSegmentPoints(PipeConfig config, ref List<Vector3>[] allPipePoints, ref List<Vector3>[] allPipeNormals,
            ref List<float> distances, Vector3 start, Vector3 end,Quaternion previousRotation,
            Quaternion currentRotation, SegmentState state)
        {

            // |---
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

                AddCrossSection(config,ref allPipePoints, ref allPipeNormals, start, currentRotation);
            }

            float clampedMiterDistance = Mathf.Max(config.miterDistance, config.radius);
            Vector3 miteredStartPoint = GetMiteredPoint(start, end, clampedMiterDistance);
            Quaternion offsetRotation = (previousRotation * Quaternion.Inverse(currentRotation));
            Vector3 previousDirection = offsetRotation * (end - start).normalized;
            Vector3 currentDirection = (end - start).normalized;
            Vector3 miterPivotPointOffset = Vector3.SlerpUnclamped(-previousDirection, currentDirection, 0.5f) * (Mathf.Cos(Vector3.Angle(previousDirection, currentDirection) * Mathf.Deg2Rad) * clampedMiterDistance);
            
            Debug.DrawRay(start + miterPivotPointOffset,miterPivotPointOffset,Color.white,1f);
            // \---
            if (state.HasFlag(SegmentState.MiteredStart))
            {
                
                if (config.miterSteps > 0)
                {
                    distances.Add(distances[^1] + config.miterDistance);
                    Vector3 midpoint = GetMiteredPoint(start, end, 0);
                    midpoint = (start + miterPivotPointOffset) +
                               Vector3.ClampMagnitude(midpoint- (start + miterPivotPointOffset),
                                   clampedMiterDistance);
                    AddCrossSection(config, ref allPipePoints, ref allPipeNormals,midpoint ,Quaternion.Slerp(previousRotation,currentRotation,0.5f));
                }
                
                distances.Add(distances[^1] + (config.miterDistance));
                AddCrossSection(config,ref allPipePoints, ref allPipeNormals, miteredStartPoint,
                    currentRotation);
            }

            // ---/
            if (state.HasFlag(SegmentState.MiteredEnd))
            {
                distances.Add(distances[^1] + (Vector3.Distance(start, end) - config.miterDistance));
                AddCrossSection(config,ref allPipePoints, ref allPipeNormals, GetMiteredPoint(end, start, clampedMiterDistance),
                    currentRotation);
            }

            // ---|
            if (state.HasFlag(SegmentState.End))
            {
                distances.Add(distances[^1] + Vector3.Distance(start, end));
                AddCrossSection(config,ref allPipePoints, ref allPipeNormals, end, currentRotation);
            }
        }

        private static void AddCrossSection(PipeConfig config, ref List<Vector3>[] allPipePoints, ref List<Vector3>[] allPipeNormals,
            Vector3 position,
            Quaternion rotation)
        {
            var crossSectionPoints = GetPipeCrossSection(config,position, rotation);
            for (int j = 0; j < config.sides; j++)
            {
                allPipePoints[j].Add(crossSectionPoints[j]);
                allPipeNormals[j].Add((crossSectionPoints[j] - position).normalized);
            }
        }
        
        
        
        private static int GetVertexIndex(PipeConfig config, int i, int j)
        {
            return (i * (config.sides + 1)) + j;
        }
        
        public static Vector3 GetMiteredPoint(Vector3 start, Vector3 end, float distance)
        {
            return start + Vector3.ClampMagnitude(end - start, distance);
        }

        public static Vector2[] GetRegularPolygon(PipeConfig config, float angleOffset = 0)
        {
            var points = new Vector2[config.sides];
            for (int i = 0; i < config.sides; i++)
            {
                points[i] = Quaternion.AngleAxis(((i / (float)config.sides) * 360f) + angleOffset, Vector3.forward) *
                            (Vector2.right * config.radius);
            }

            return points;
        }

        public static Vector3[] GetPipeCrossSection(PipeConfig config, Vector3 position, Quaternion direction)
        {
            var basePoints = GetRegularPolygon(config, (0.5f / config.sides) * 360);
            var outPoints = basePoints.Select(point => (direction * point) + position).ToArray();
            return outPoints;
        }
    }
}