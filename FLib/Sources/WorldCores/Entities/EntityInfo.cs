// ==================== qcbf@qq.com | 2026-01-04 ====================

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    [SkipLocalsInit]
    public struct EntityInfo
    {
        public static EntityInfo Empty = default;
        public Chunk Chunk;
        public readonly ushort Version;
        public ushort IndexInChunk;
        private ushort _dynamicComponentIndex;
        private ushort _destroyFlagWithArchetypeIndex;

        public bool IsEmpty => Version == 0;

        /// <summary>
        /// 
        /// </summary>
        public readonly int ArchetypeIndex => _destroyFlagWithArchetypeIndex & 0x7fff;

        /// <summary>
        /// 
        /// </summary>
        public readonly bool IsDestroying => (_destroyFlagWithArchetypeIndex & 0x8000) == 0;

        /// <summary>
        /// 
        /// </summary>
        public int DynamicComponentSparseIndex
        {
            readonly get => _dynamicComponentIndex - 1;
            set => _dynamicComponentIndex = checked((ushort)(value + 1));
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly bool HasDynamicComponent => _dynamicComponentIndex != 0;

        public EntityInfo(ushort version, ushort archetypeIndex, ushort indexInChunk, Chunk chunk)
        {
            Debug.Assert((archetypeIndex & 0x8000) == 0);
            Version = version;
            _destroyFlagWithArchetypeIndex = (ushort)(archetypeIndex | 0x8000);
            IndexInChunk = indexInChunk;
            Chunk = chunk;
            _dynamicComponentIndex = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly Archetype GetArchetype(WorldCore world) => world.ArchetypeGroup[ArchetypeIndex];

        /// <summary>
        /// 
        /// </summary>
        public void SetDestroying()
        {
            _destroyFlagWithArchetypeIndex &= 0x7fff;
        }
    }
}