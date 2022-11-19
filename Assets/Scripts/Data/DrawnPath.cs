using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DrawnPath
{
    [SerializeField] private List<Vector3Int> points = new List<Vector3Int>();
    public UnityEvent onUpdate;
    public PipesGrid PipesGrid;

    public void Clear()
    {
        points.Clear();
    }

    public void AddPoint(Vector3Int point)
    {
        if (points.Contains(point))
        {
            int startIndex = points.IndexOf(point);
            points.RemoveRange(startIndex + 1, points.Count - (startIndex + 1));
        }
        else
        {
            points.Add(point);
        }

        onUpdate?.Invoke();
    }
}