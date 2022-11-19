using UnityEngine;

namespace Utility
{
    public static class GridCollisionChecking
    {
        //this gives the flattened index of the neighbor offset vector
        private static int GetOffsetIndex(Vector3Int v)
        {
            int offsetIndex = ((v.x + 1) * 9) + ((v.y + 1) * 3) + v.z + 1;
            if (offsetIndex > 12)
            {
                offsetIndex--;
            }

            return offsetIndex;
        }


        private static int ApplyCollisionMask(Vector3Int v, Vector3Int maskDirection)
        {
            if (maskDirection != v && maskDirection != Vector3Int.zero)
            {
                return 1 << GetOffsetIndex(maskDirection);
            }

            return 0;
        }

        //this creates a mask that includes cardinal directions in collision checks,
        //forcing pipes to go around corners rather than cutting across them 
        /*
    
        _ _ +
        _ O X
        _ _ _
        
        O: Current Location
        +: Testing Location
        X: Collision Location
        */
        public static int GetCornerCollisionMask(this Vector3Int v)
        {
            int mask = 0;

            mask |= ApplyCollisionMask(v, new Vector3Int(v.x, 0, 0));
            mask |= ApplyCollisionMask(v, new Vector3Int(0, v.y, 0));
            mask |= ApplyCollisionMask(v, new Vector3Int(0, 0, v.z));
            mask |= ApplyCollisionMask(v, new Vector3Int(v.x, v.y, 0));
            mask |= ApplyCollisionMask(v, new Vector3Int(0, v.y, v.z));
            mask |= ApplyCollisionMask(v, new Vector3Int(v.x, 0, v.z));

            return mask;
        }
    }
}