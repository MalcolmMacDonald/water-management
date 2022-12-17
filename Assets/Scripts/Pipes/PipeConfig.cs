using System;

namespace Pipes
{
    [Serializable]
    public struct PipeConfig
    {
        public int sides;
        public float radius;
        public float miterDistance;
        public int miterSteps;
        public float miterPower;
    }
}