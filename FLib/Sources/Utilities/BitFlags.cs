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
    /// 按位标记
    /// 上限：16(0-15)(4bit)个标记组，每个组27(bit)个标记
    /// </summary>
    public struct BitFlags : IBytesSerializable, IJson5Deserializable, IJson5Serializable, IEquatable<BitFlags>
    {
        #region static
        
        /// <summary>
        /// 包含全部标记
        /// </summary>
        public static readonly BitFlags AllFlags = uint.MaxValue;
        
        /// <summary>
        /// 没有任何标记
        /// </summary>
        public static readonly BitFlags EmptyFlags = 0;
        
        /// <summary>
        /// 标记组的字符串名称记录. 数组下标代表组id
        /// [groupId{flagName,flagBitMask}]
        /// </summary>
        public static ReadOnlyDictionary<string, byte>[] FlagNameBits;
        
        /// <summary>
        /// 标记组的字符串名称记录
        /// [[group0Flag0,group0Flag1], [group1Flag0,group1Flag1]]
        /// </summary>
        public static string[][] FlagGroupNames;
        
        /// <summary>
        /// 是否已经初始化过从外部得到的标记名字配置
        /// </summary>
        public static bool IsInitialized => FlagNameBits != null;
        
        /// <summary>
        /// 初始化全部标记名称配置
        /// </summary>
        /// <param name="flagGroupNames"> [groupName[flagName,flagName] ] </param>
        public static void Initialize(string[][] flagGroupNames)
        {
            Log.Assert(flagGroupNames.Length <= 15);
            FlagNameBits = new ReadOnlyDictionary<string, byte>[flagGroupNames.Length];
            FlagGroupNames = flagGroupNames;
            for (var group = 0; group < flagGroupNames.Length; group++)
            {
                var flagNames = flagGroupNames[group];
                if (flagNames == null)
                    continue;
                var dict = new Dictionary<string, byte>();
                for (var i = 0; i < flagNames.Length; i++)
                    dict.TryAdd(flagNames[i], (byte)i);
                FlagNameBits[group] = new ReadOnlyDictionary<string, byte>(dict);
            }
        }
        
        /// <summary>
        /// 获取标记二进制位, 不包含组
        /// </summary>
        public static byte GetFlagBit(byte group, string name)
        {
            if (group >= FlagNameBits.Length || !FlagNameBits[group].TryGetValue(name, out var bit))
                throw new Exception($"not found flag: {group}>{name}");
            return bit;
        }
        
        /// <summary>
        /// 或者完整标记标记数据
        /// </summary>
        public static BitFlags GetFlag(byte group, string name)
        {
            return new BitFlags(group, 1u << GetFlagBit(group, name));
        }
        
        #endregion
        
        #region instance
        
        public uint Raw;
        
        public byte Group
        {
            readonly get => (byte)(Raw >> 28);
            set => Raw = (Raw & 0x0FFFFFFFu) | ((uint)(value & 0xF) << 28);
        }
        
        public uint Mask
        {
            readonly get => Raw & 0x0FFFFFFFu;
            set => Raw = (value & 0x0FFFFFFFu) | ((uint)Group << 28);
        }
        
        /// <summary>
        /// 是否包含指定的全部标记(忽略group)
        /// </summary>
        public readonly bool All(uint mask) => (Mask & mask) == mask;
        
        /// <summary>
        /// 是否包含指定的任意一个标记(忽略group)
        /// </summary>
        public readonly bool Any(uint mask) => (Mask & mask) != 0;
        
        /// <summary>
        /// 是否包含指定的全部标记
        /// </summary>
        public readonly bool All(in BitFlags flags) => Group == flags.Group && (Mask & flags.Mask) == flags.Mask;
        
        /// <summary>
        /// 是否包含指定的任意一个标记
        /// </summary>
        public readonly bool Any(in BitFlags flags) => Group == flags.Group && (Mask & flags.Mask) != 0;
        
        /// <summary>
        /// 添加指定标记
        /// </summary>
        public void Add(in BitFlags flags)
        {
            Log.Assert(flags.Group == Group)?.Write($"group error add({flags}), cur: {this}");
            Mask |= flags.Mask;
        }
        
        /// <summary>
        /// 移除指定标记
        /// </summary>
        public void Remove(in BitFlags flags)
        {
            Log.Assert(flags.Group == Group)?.Write($"group error {this} remove({flags})");
            Mask &= ~flags.Mask;
        }
        
        public readonly bool IsEmpty => Mask == 0;
        public readonly override string ToString() => $"{Group}:{Mask}";
        public BitFlags(byte group, uint mask) => Raw = ((uint)(group & 0xF) << 28) | (mask & 0x0FFFFFFFu);
        
        #endregion
        
        #region serialization
        
        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
        {
            if (nodes.TryMoveNextValueOrCloseToken(out var node))
            {
                var flagGroup = node.ContentCopyString.ToByte();
                if (flagGroup > 15 || flagGroup >= FlagNameBits.Length)
                    throw new Exception($"flag group error: {flagGroup}");
                var nameBits = FlagNameBits[flagGroup];
                while (nodes.TryMoveNextValueOrCloseToken(out node))
                {
                    if (!nameBits.TryGetValue(node.ContentCopyString, out var bit))
                        throw new Exception($"not found flag name: {flagGroup}>{node.ContentCopyString}");
                    Raw |= (uint)1 << bit;
                }
                
                Raw = Raw << 4 | flagGroup;
                return true;
            }
            
            return false;
        }
        
        public readonly string JsonSerialize(object serializeObject, object customData, int indent, Json5SerializeOptionData opData)
        {
            if (Raw == 0)
                return string.Empty;
            if (FlagGroupNames == null || FlagGroupNames.Length == 0)
                return $"[{Group},{Mask}]";
            
            var allFlagNames = FlagGroupNames[Group];
            using var names = new PooledList<string>();
            var mask = Mask;
            for (var i = 0; mask != 0; i++)
            {
                if ((mask & 1) != 0)
                    names.Add(allFlagNames[i]);
                mask >>= 1;
            }
            
            return names.IsEmpty ? $"[{Group}]" : $"[{Group},{string.Join(',', names)}]";
        }
        
        #endregion
        
        #region helper
        
        public static implicit operator uint(in BitFlags flags) => flags.Raw;
        public static implicit operator int(in BitFlags flags) => (int)flags.Raw;
        public static implicit operator long(in BitFlags flags) => flags.Raw;
        public static implicit operator ushort(in BitFlags flags) => (ushort)flags.Raw;
        public static implicit operator short(in BitFlags flags) => (short)flags.Raw;
        public static implicit operator BitFlags(uint flags) => new() { Raw = flags };
        public static implicit operator BitFlags(int flags) => new() { Raw = (uint)flags };
        public static implicit operator BitFlags(long flags) => new() { Raw = (uint)flags };
        public readonly void Z_BytesWrite(ref BytesWriter writer) => writer.PushVInt(Raw);
        public void Z_BytesRead(ref BytesReader reader) => Raw = (uint)reader.ReadVInt();
        public bool Equals(BitFlags other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is BitFlags other && Equals(other);
        public override int GetHashCode() => (int)Raw;
        public static bool operator ==(BitFlags left, BitFlags right) => left.Raw == right.Raw;
        public static bool operator !=(BitFlags left, BitFlags right) => left.Raw != right.Raw;
        
        #endregion
    }
}