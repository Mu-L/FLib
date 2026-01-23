// ==================== qcbf@qq.com | 2026-01-23 ====================

using System;

namespace FLib.WorldCores
{
    internal static class StaticComponentMask
    {
        [ThreadStatic] private static ulong[] _buffer;

        /// <summary>
        /// 
        /// </summary>
        public static void EnsureCapacity(IncrementId maxId)
        {
            var maxLen = (int)Math.Ceiling(maxId.Raw / (float)BitArrayOperator.BitSize);
            if (_buffer == null || _buffer.Length < maxLen)
                Array.Resize(ref _buffer, maxLen + GlobalSetting.CapacityExpandSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public static void Set(IncrementId id, bool value) => BitArrayOperator.SetBit(_buffer, id, value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public static bool Get(IncrementId id) => id.Raw <= _buffer.Length && BitArrayOperator.GetBit(_buffer, id);

        /// <summary>
        /// 
        /// </summary>
        public static void Clear() => Array.Clear(_buffer);

        /// <summary>
        /// 
        /// </summary>
        public static int HashCode() => ComponentRegistry.GetHash(_buffer);
    }
}