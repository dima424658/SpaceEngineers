using System;

namespace VRageMath.PackedVector
{
    internal static class PackUtils
    {
        public static uint PackUnsigned(float bitmask, float value)
        {
            return (uint)ClampAndRound(value, 0.0f, bitmask);
        }

        public static uint PackSigned(uint bitmask, float value)
        {
            float max = bitmask >> 1;
            float min = (float)(-(double)max - 1.0);
            return (uint)(int)ClampAndRound(value, min, max) & bitmask;
        }

        public static uint PackUNorm(float bitmask, float value)
        {
            value *= bitmask;
            return (uint)ClampAndRound(value, 0.0f, bitmask);
        }

        public static float UnpackUNorm(uint bitmask, uint value)
        {
            value &= bitmask;
            return value / bitmask;
        }

        public static uint PackSNorm(uint bitmask, float value)
        {
            float max = bitmask >> 1;
            value *= max;
            return (uint)ClampAndRound(value, -max, max) & bitmask;
        }

        public static float UnpackSNorm(uint bitmask, uint value)
        {
            uint num1 = bitmask + 1U >> 1;
            if ((value & num1) != 0)
            {
                if ((value & bitmask) == num1)
                    return -1f;
                value |= ~bitmask;
            }
            else
                value &= bitmask;

            return (float)(int)value / (bitmask >> 1);
        }

        private static double ClampAndRound(float value, float min, float max)
        {
            if (float.IsNaN(value))
                return 0.0;
            if (float.IsInfinity(value))
                return float.IsNegativeInfinity(value) ? min : max;
            if (value < min)
                return min;
            if (value > max)
                return max;
            else
                return Math.Round(value);
        }
    }
}
