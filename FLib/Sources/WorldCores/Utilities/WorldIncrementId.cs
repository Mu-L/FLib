// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;

namespace FLib.WorldCores
{
    public readonly struct WorldIncrementId : IEquatable<WorldIncrementId>
    {
        public readonly ushort Raw;
        public ushort Id => (ushort)(Raw - 1);
        public bool IsEmpty => Raw == 0;
        public WorldIncrementId(ushort raw) => Raw = raw;
        public WorldIncrementId(int raw) => Raw = checked((ushort)raw);
        public override string ToString() => Id.ToString();
        public static implicit operator ushort(in WorldIncrementId id) => id.Id;
        public static implicit operator int(in WorldIncrementId id) => id.Id;
        public bool Equals(WorldIncrementId other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is WorldIncrementId other && Equals(other);
        public override int GetHashCode() => Raw.GetHashCode();
        public static bool operator ==(WorldIncrementId left, WorldIncrementId right) => left.Raw == right.Raw;
        public static bool operator !=(WorldIncrementId left, WorldIncrementId right) => !(left == right);
        public static bool operator >(WorldIncrementId left, WorldIncrementId right) => left.Raw > right.Raw;
        public static bool operator <(WorldIncrementId left, WorldIncrementId right) => left.Raw < right.Raw;
    }
}