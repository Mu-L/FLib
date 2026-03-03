// ==================== qcbf@qq.com | 2026-03-03 ====================

using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public readonly struct WorldHandle
    {
        public readonly ushort Index;
        public readonly ushort Version;


        public WorldCore World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WorldCore.AllWorlds[Index];
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Version == 0 || WorldCore.AllWorlds.Count <= Index || WorldCore.AllWorlds[Index]?.Handle.Version != Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator WorldCore(in WorldHandle v) => v.World;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WorldHandle(ushort index, ushort version)
        {
            Index = index;
            Version = version;
        }
    }
}