// ==================== qcbf@qq.com | 2026-03-03 ====================

using System;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public readonly struct WorldHandle : IEquatable<WorldHandle>
    {
        public readonly ushort Index;
        public readonly ushort Version;


        public WorldCore World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => WorldCore.AllWorlds[Index];
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Version == 0 || WorldCore.AllWorlds.Count <= Index || WorldCore.AllWorlds[Index]?.Handle.Version != Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator WorldCore(in WorldHandle v) => v.World;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WorldHandle(ushort index, ushort version)
        {
            Index = index;
            Version = version;
        }

        public override int GetHashCode() => HashCode.Combine(Index, Version);
        public override string ToString() => $"{Index},{Version}";

        public static bool operator ==(in WorldHandle left, in WorldHandle right) => left.Index == right.Index && left.Version == right.Version;
        public static bool operator !=(in WorldHandle left, in WorldHandle right) => left.Index != right.Index || left.Version != right.Version;
        public bool Equals(WorldHandle other) => Index == other.Index && Version == other.Version;
        public override bool Equals(object obj) => obj is WorldHandle other && Equals(other);
    }
}