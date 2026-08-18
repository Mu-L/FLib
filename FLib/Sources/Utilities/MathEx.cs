// ==================={By Qcbf|qcbf@qq.com|5/28/2021 5:33:39 PM}===================

// #undef FNUM

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib
{
    public static class MathEx
    {
        public static readonly FNum Epsilon = FNum.Epsilon;
        public static readonly FNum Pi = FNum.PI;
        public static readonly FNum Deg2Rad = Pi / 180;
        public static readonly FNum Rad2Deg = 1 / Deg2Rad;

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetNextCapacityLength(int currentLength) =>
            currentLength < 4096
                ? GetNextPowerOfTwo(currentLength + 1)
                : currentLength + (currentLength < 16384 ? currentLength >> 1 : Math.Max(8192, currentLength >> 2));

        /// <summary>
        /// 如果当前不是2的次幂寻找下一个2的次幂的数字
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetNextPowerOfTwo(int v)
        {
            --v;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            return ++v;
        }


        /// <summary> 取 value 按 alignment 向上对齐后的值. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignUp(int value, int alignment)
        {
            var mask = alignment - 1;
            return (value + mask) & ~mask;
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Clamp(long value, long min, long max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FNum Lerp(FNum from, FNum to, FNum t) => from + (to - from) * t;

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FNum Clamp01(FNum value)
        {
            if (value < 0)
                return 0;
            return value > 1 ? 1 : value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FNum Clamp(FNum value, FNum min, FNum max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }
    }
}