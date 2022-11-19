using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [Serializable]
    public struct ContinuousGrid<T> : IGrid<T> where T : ICell
    {
        public int width;
        public int height;
        public int depth;
        public T[] flatGrid;

        private BoundsInt bounds;

        public ContinuousGrid(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
            bounds = new BoundsInt(0, 0, 0, width, height, depth);
            flatGrid = new T[width * height * depth];
        }

        public bool Initialized()
        {
            return flatGrid != null;
        }

        public T this[int x, int y, int z]
        {
            get => GetValue(x, y, z);
            set => SetValue(x, y, z, value);
        }

        public T this[Vector3Int v]
        {
            get => GetValue(v.x, v.y, v.z);
            set => SetValue(v.x, v.y, v.z, value);
        }

        public bool GetValueSafe(Vector3Int position, out T cell)
        {
            if (!bounds.Contains(position))
            {
                cell = default;
                return false;
            }

            cell = this[position];
            return true;
        }

        public bool HasValue(Vector3Int position)
        {
            if (!GetValueSafe(position, out T value))
            {
                return false;
            }

            if (!value.IsActive())
            {
                return false;
            }

            return true;
        }

        public IEnumerable<T> GetEnumerable()
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < depth; k++)
                    {
                        yield return this[i, j, k];
                    }
                }
            }
        }

        private void SetValue(int x, int y, int z, T value)
        {
            flatGrid[ArrayIndex(x, y, z)] = value;
        }

        private T GetValue(int x, int y, int z)
        {
            return flatGrid[ArrayIndex(x, y, z)];
        }

        private int ArrayIndex(int x, int y, int z)
        {
            return x + (y * width) + (z * width * height);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(flatGrid, bounds);
        }

        public static bool operator ==(ContinuousGrid<T> a, ContinuousGrid<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ContinuousGrid<T> a, ContinuousGrid<T> b)
        {
            return !(a == b);
        }
    }
}