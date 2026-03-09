// ==================== qcbf@qq.com |2026-01-02 ====================

using System;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    public class WorldArchetypeGroup
    {
        public readonly WorldCore World;
        public WorldArchetype[] Archetypes;
        public readonly Dictionary<int, WorldArchetype> ArchetypeMap;

        public ushort Count => (ushort)ArchetypeMap.Count;
        public WorldArchetype this[int key] => ArchetypeMap[key];
        public WorldArchetype this[ushort index] => Archetypes[index];

        public WorldArchetypeGroup(WorldCore world, int capacity = 16)
        {
            Archetypes = new WorldArchetype[capacity];
            ArchetypeMap = new Dictionary<int, WorldArchetype>(capacity);
            World = world;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldArchetype Create(int hash, in WorldArchetypeBuilder builder)
        {
            var index = Count;
            var archetype = new WorldArchetype(World, builder, index);
            if (Archetypes.Length <= index)
                Array.Resize(ref Archetypes, MathEx.GetNextPowerOfTwo(index + 1));
            ArchetypeMap.Add(hash, Archetypes[index] = archetype);
            return archetype;
        }
    }
}