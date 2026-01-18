// ==================== qcbf@qq.com | 2026-01-17 ====================

using System;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    public readonly struct QueryFilter
    {
        internal readonly Archetype[] Archetypes;
        internal readonly QuerySharedComponent[] SharedComponents;

        public bool IsEmpty => Archetypes == null;

        public QueryFilter(WorldCore world, in QueryFilterBuilder builder)
        {
            using var archetypes = new PooledList<Archetype>();
            for (ushort i = 0; i < world.ArchetypeGroup.Count; i++)
            {
                var archetype = world.ArchetypeGroup[i];
                if (builder.Match(archetype))
                    archetypes.Add(archetype);
            }

            Archetypes = archetypes.ToArray();
            SharedComponents = builder.SharedComponents?.ToArray() ?? Array.Empty<QuerySharedComponent>();
        }

        // /// <summary>
        // /// 
        // /// </summary>
        // public void Combine(in QueryFilter filter)
        // {
        //     if (filter.Archetypes?.Length > 0)
        //     {
        //         using var archetypeSets = new PooledHashSet<Archetype>();
        //         archetypeSets.Raw.EnsureCapacity(Archetypes.Length + filter.Archetypes.Length);
        //         for (var i = 0; i < Archetypes.Length; i++)
        //             archetypeSets.Add(Archetypes[i]);
        //         for (var i = 0; i < filter.Archetypes.Length; i++)
        //         {
        //             if (archetypeSets.Add(filter.Archetypes[i]))
        //         }
        //     }
        // }
    }
}