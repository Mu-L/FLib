// ==================== qcbf@qq.com |2025-12-11 ====================

using System;
using System.Runtime.CompilerServices;
using System.Text;
using FLib.WorldCores;

namespace FLib.WorldCores.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct WorldEntityId : IEquatable<WorldEntityId>, IJson5Serializable
    {
        /// <summary> start from zeron </summary>
        public readonly ushort Id;

        /// <summary> start from one, nonzero </summary>
        public readonly ushort Version;

        public bool IsEmpty => Version == 0;
        public override string ToString() => ((uint)this).ToString();

        public WorldEntityId(ushort id, ushort version)
        {
            Id = id;
            Version = version;
        }

        bool IJson5Serializable.JsonSerialize(StringBuilder jsonText, object serializeObject, object customData, int indent, Json5SerializeOptionData opData)
        {
            jsonText.Append(ToString());
            return true;
        }

        public WorldEntity AsEntity(WorldHandle world) => new(world, this);
        public bool Equals(WorldEntityId other) => this == other;
        public override bool Equals(object obj) => obj is WorldEntityId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Version);
        public static bool operator ==(in WorldEntityId left, in WorldEntityId right) => left.Id == right.Id && left.Version == right.Version;
        public static bool operator !=(in WorldEntityId left, in WorldEntityId right) => left.Id != right.Id || left.Version != right.Version;
        public static implicit operator uint(in WorldEntityId id) => (uint)(id.Id << 16) | id.Version;
        public static implicit operator int(in WorldEntityId id) => (id.Id << 16) | id.Version;
    }
}