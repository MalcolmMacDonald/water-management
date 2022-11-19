using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class HashedGrid<T> : ISerializationCallbackReceiver, IGrid<T> where T : ICell
    {
        [SerializeField] public Vector3Int[] serializedKeys;

        [SerializeField] public T[] serializedCells;

        private Dictionary<Vector3Int, T> grid;
        private HashSet<Vector3Int> keySet;

        public HashedGrid()
        {
            grid = new Dictionary<Vector3Int, T>();
            keySet = new HashSet<Vector3Int>();
        }

        public int elementCount => grid.Count;

        public T this[int x, int y, int z]
        {
            get => grid[new Vector3Int(x, y, z)];
            set => SetValue(new Vector3Int(x, y, z), value);
        }

        public T this[Vector3Int v]
        {
            get => grid[v];
            set => SetValue(v, value);
        }

        public bool Initialized()
        {
            return grid != null && grid.Count > 0;
        }


        public bool HasValue(Vector3Int position)
        {
            return keySet.Contains(position) && grid[position].IsActive();
        }

        public bool GetValueSafe(Vector3Int position, out T value)
        {
            value = default;
            if (!HasValue(position))
            {
                return false;
            }


            value = grid[position];
            return true;
        }

        public IEnumerable<T> GetEnumerable()
        {
            var keyCopy = new List<Vector3Int>(grid.Keys);
            foreach (Vector3Int key in keyCopy) yield return grid[key];
        }

        public void OnBeforeSerialize()
        {
            serializedKeys = grid.Keys.ToArray();
            serializedCells = grid.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            if (HasSerializedData() && !Initialized())
            {
                grid = new Dictionary<Vector3Int, T>();
                for (int i = 0; i < serializedKeys.Length; i++)
                {
                    grid.Add(serializedKeys[i], serializedCells[i]);
                }

                serializedKeys = null;
                serializedCells = null;
            }
            else
            {
                grid = new Dictionary<Vector3Int, T>();
            }

            keySet = new HashSet<Vector3Int>(grid.Keys);
        }

        public bool HasSerializedData()
        {
            return serializedKeys != null && serializedKeys.Length > 0;
        }

        private void SetValue(Vector3Int v, T value)
        {
            grid[v] = value;
            keySet.Add(v);
        }

        public void AddRange(IEnumerable<Vector3Int> keys, IEnumerable<T> values)
        {
            grid.AddRange(keys.Zip(values, (key, value) => new KeyValuePair<Vector3Int, T>(key, value)));
        }

        public void TrimExcess()
        {
            grid.TrimExcess();
        }

        public void Remove(Vector3Int v)
        {
            grid.Remove(v);
            keySet.Remove(v);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return grid.GetHashCode();
        }

        public int GetElementCount()
        {
            return grid == null ? 0 : grid.Count;
        }

        public static bool operator ==(HashedGrid<T> a, HashedGrid<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(HashedGrid<T> a, HashedGrid<T> b)
        {
            return !(a == b);
        }
    }
}