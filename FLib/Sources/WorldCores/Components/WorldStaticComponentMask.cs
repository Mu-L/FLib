// ==================== qcbf@qq.com | 2026-01-23 ====================

using System;
using FLib.WorldCores;

namespace FLib.WorldCores.Components
{
    internal static class WorldStaticComponentMask
    {
        [ThreadStatic] private static ulong[] _buffer;

        /// <summary>
        /// 
        /// </summary>
        public static void EnsureCapacity(WorldIncrementId maxId)
        {
            var maxLen = (int)Math.Ceiling(maxId.Raw / (float)BitArrayOperator.BitSize);
            if (_buffer == null || _buffer.Length < maxLen)
                Array.Resize(ref _buffer, maxLen + WorldSetting.ComponentTypeCapacityExpandSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public static void Set(WorldIncrementId id, bool value) => BitArrayOperator.SetBit(_buffer, id, value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public static bool Get(WorldIncrementId id) => !id.IsEmpty && _buffer != null && BitArrayOperator.GetBitsLength(id.Raw) <= _buffer.Length && BitArrayOperator.GetBit(_buffer, id);

        /// <summary>
        /// 
        /// </summary>
        public static void Clear()
        {
            if (_buffer != null)
                Array.Clear(_buffer, 0, _buffer.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        public static int HashCode() => WorldComponentRegistry.GetHash(_buffer);
    }
}