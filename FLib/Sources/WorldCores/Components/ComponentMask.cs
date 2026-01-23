// // ==================== qcbf@qq.com | 2026-01-23 ====================
//
// using System;
// using System.Diagnostics;
// using System.Runtime.InteropServices;
//
// namespace FLib.WorldCores
// {
//     public struct ComponentMask
//     {
//         [ThreadStatic] internal static ComponentMask Cache;
// #if DEBUG
//         [ThreadStatic] private static bool _used;
// #endif
//         public ulong[] Mask;
//
//         public readonly bool IsEmpty => Mask == null;
//
//         public ComponentMask(IncrementId maxId)
//         {
//             Mask = new ulong[maxId.Raw];
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         /// <param name="id"></param>
//         /// <param name="value"></param>
//         public void Set(IncrementId id, bool value) => BitArrayOperator.SetBit(Mask, id, value);
//
//         /// <summary>
//         /// 
//         /// </summary>
//         /// <param name="maxId"></param>
//         /// <param name="value"></param>
//         public void SafeSet(IncrementId maxId, bool value)
//         {
//             EnsureCapacity(maxId);
//             BitArrayOperator.SetBit(Mask, maxId, value);
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         /// <param name="id"></param>
//         public bool Get(IncrementId id) => Mask != null && id.Raw <= Mask.Length && BitArrayOperator.GetBit(Mask, id);
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public void EnsureCapacity(IncrementId maxId)
//         {
//             var maxLen = (int)Math.Ceiling(maxId.Raw / (float)BitArrayOperator.BitSize);
//             if (Mask == null || Mask.Length < maxLen)
//                 Array.Resize(ref Mask, maxLen + GlobalSetting.CapacityExpandSize);
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public static ComponentMask Rent()
//         {
// #if DEBUG
//             Debug.Assert(!_used);
//             _used = true;
// #endif
//             return Cache;
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public static void Release()
//         {
// #if DEBUG
//             Debug.Assert(_used);
//             _used = false;
// #endif
//             Cache.Mask.AsSpan().Clear();
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public override int GetHashCode()
//         {
//             var hash = new HashCode();
//             hash.AddBytes(MemoryMarshal.AsBytes(Mask.AsSpan()));
//             return hash.ToHashCode();
//         }
//     }
// }