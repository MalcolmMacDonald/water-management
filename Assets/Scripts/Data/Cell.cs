using System;
using UnityEngine;

namespace Data
{
    [Serializable]
    public struct Cell : ICell
    {
        public Vector3Int position;
        public CellState cellState;
        public double cost;
        public double pathCostSoFar;
        public int neighborMask;


        public string maskDebugString;

        public int neighborCount;

        public bool IsActive()
        {
            return cellState == CellState.Empty;
        }


        public bool MaskHasNeighborIndex(int index)
        {
            return (neighborMask & (1 << index)) != 0;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return position.GetHashCode();
        }

        public static bool operator ==(Cell a, Cell b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Cell a, Cell b)
        {
            return !(a == b);
        }
    }

    public enum CellState
    {
        Empty,
        Wall
    }
}