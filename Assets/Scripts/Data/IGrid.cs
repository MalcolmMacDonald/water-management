using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    public interface IGrid
    {
        public bool HasValue(Vector3Int position);
    }

    public interface IGrid<T> : IGrid where T : ICell
    {
        public T this[int x, int y, int z] { get; set; }
        public T this[Vector3Int v] { get; set; }
        public bool GetValueSafe(Vector3Int position, out T value);
        public bool Initialized();
        public IEnumerable<T> GetEnumerable();
    }
}