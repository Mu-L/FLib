// ==================== qcbf@qq.com | 2026-03-14 ====================

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib
{
    public static class UnityCompatibility
    {
#if !NET6_0_OR_GREATER
        public static void AddBytes(this ref HashCode hash, in ReadOnlySpan<byte> bytes)
        {
            ref var ptr = ref MemoryMarshal.GetReference(bytes);
            var len = bytes.Length;
            var i = 0;
            for (; i <= len - 8; i += 8)
                hash.Add(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref ptr, i)));
            if ((len & 4) != 0)
            {
                hash.Add(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref ptr, i)));
                i += 4;
            }

            if ((len & 2) != 0)
            {
                hash.Add(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref ptr, i)));
                i += 2;
            }

            if ((len & 1) != 0)
            {
                hash.Add(Unsafe.Add(ref ptr, i));
            }
        }
#endif
    }

#if !NET6_0_OR_GREATER
    namespace System.Runtime.CompilerServices
    {
        [Conditional("DEBUG")]
        public class SkipLocalsInitAttribute : Attribute
        {
        }
    }
#endif
}