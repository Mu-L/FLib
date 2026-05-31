// ==================== qcbf@qq.com | 2026-01-04 ====================

using System.Diagnostics;
using System.Runtime.CompilerServices;
using FLib.WorldCores;
using FLib.WorldCores.Archetypes;

namespace FLib.WorldCores.Entities
{
    public struct WorldEntityInfo
    {
        public static WorldEntityInfo Empty = default;
        public WorldChunk Chunk;
        public readonly ushort Version;
        public ushort IndexInChunk;
        private ushort _dynamicComponentIndex;
        private ushort _destroyFlagWithArchetypeIndex;

        public bool IsEmpty => Version == 0;

        /// <summary>
        /// 
        /// </summary>
        public readonly ushort ArchetypeIndex => (ushort)(_destroyFlagWithArchetypeIndex & 0x7fff);

        public readonly bool IsDestroying => (_destroyFlagWithArchetypeIndex & 0x8000) == 0;

        public override string ToString() => $"{Version},{IndexInChunk},{_dynamicComponentIndex}";

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

        public WorldEntityInfo(ushort version, ushort archetypeIndex, ushort indexInChunk, WorldChunk chunk)
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
        public readonly WorldArchetype GetArchetype(WorldCore world) => world.ArchetypeGroup[ArchetypeIndex];

        /// <summary>
        /// 
        /// </summary>
        public void SetDestroying()
        {
            _destroyFlagWithArchetypeIndex &= 0x7fff;
        }
    }
}