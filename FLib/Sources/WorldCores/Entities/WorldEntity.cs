// ==================== qcbf@qq.com |2025-12-11 ====================

using System;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    /// <summary>
    /// 
    /// </summary>
    [SkipLocalsInit]
    public readonly struct WorldEntity : IEquatable<WorldEntity>
    {
        public readonly ushort Id;
        public readonly ushort Version;
        public bool IsEmpty => Version == 0;
        public override string ToString() => Id.ToString();

        public WorldEntity(ushort id, ushort version)
        {
            Id = id;
            Version = version;
        }

        public WorldEntityHelper AsHelper(WorldHandle world) => new(world, this);
        public bool Equals(WorldEntity other) => this == other;
        public override bool Equals(object obj) => obj is WorldEntity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Version);
        public static bool operator ==(in WorldEntity left, in WorldEntity right) => left.Id == right.Id && left.Version == right.Version;
        public static bool operator !=(in WorldEntity left, in WorldEntity right) => left.Id != right.Id || left.Version != right.Version;
    }
}