// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FLib
{
    /// <summary>
    /// 
    /// </summary>
    [Conditional("DEBUG")]
    public class BitFlagsTypeAttribute : Attribute
    {
        public readonly Type FlagEnumType;
        
        
        public BitFlagsTypeAttribute()
        {
        }
        
        public BitFlagsTypeAttribute(Type flagEnumType)
        {
            FlagEnumType = flagEnumType;
        }
    }
    
    /// <summary>
    /// 按位标记
    /// </summary>
    public struct BitFlags : IBytesSerializable, IEquatable<BitFlags>
    {
        public uint Mask;
        
        /// <summary>
        /// 是否包含指定的全部标记(忽略group)
        /// </summary>
        public readonly bool All(uint mask) => (Mask & mask) == mask;
        
        /// <summary>
        /// 是否包含指定的任意一个标记(忽略group)
        /// </summary>
        public readonly bool Any(uint mask) => (Mask & mask) != 0;
        
        /// <summary>
        /// 添加指定标记
        /// </summary>
        public void Add(in uint mask)
        {
            Mask |= mask;
        }
        
        /// <summary>
        /// 移除指定标记
        /// </summary>
        public void Remove(in uint mask)
        {
            Mask &= ~mask;
        }
        
        public readonly bool IsEmpty => Mask == 0;
        public readonly override string ToString() => $"{Mask}";
        public BitFlags(uint mask) => Mask = mask;
        
        #region helper
        
        public static implicit operator uint(in BitFlags flags) => flags.Mask;
        public static implicit operator int(in BitFlags flags) => (int)flags.Mask;
        public static implicit operator long(in BitFlags flags) => flags.Mask;
        public static implicit operator ushort(in BitFlags flags) => (ushort)flags.Mask;
        public static implicit operator short(in BitFlags flags) => (short)flags.Mask;
        public static implicit operator BitFlags(uint flags) => new() { Mask = flags };
        public static implicit operator BitFlags(int flags) => new() { Mask = (uint)flags };
        public static implicit operator BitFlags(long flags) => new() { Mask = (uint)flags };
        public readonly void Z_BytesWrite(ref BytesWriter writer) => writer.PushVInt(Mask);
        public void Z_BytesRead(ref BytesReader reader) => Mask = (uint)reader.ReadVInt();
        public bool Equals(BitFlags other) => Mask == other.Mask;
        public override bool Equals(object obj) => obj is BitFlags other && Equals(other);
        public override int GetHashCode() => (int)Mask;
        public static bool operator ==(BitFlags left, BitFlags right) => left.Mask == right.Mask;
        public static bool operator !=(BitFlags left, BitFlags right) => left.Mask != right.Mask;
        
        #endregion
    }
}