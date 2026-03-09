// ==================== qcbf@qq.com | 2026-01-09 ====================

using System;
using System.Collections.Generic;
using FLib.WorldCores;
using FLib.WorldCores.Archetypes;
using FLib.WorldCores.Components;

namespace FLib.WorldCores.Queries
{
    public struct WorldQueryFilterBuilder
    {
        public readonly WorldCore World;
        internal ulong[] AllMask;
        internal ulong[] AnyMask;
        internal ulong[] NoneMask;
        internal List<WorldQuerySharedComponent> SharedComponents;

        public bool IsEmpty => AllMask == null && AnyMask == null && NoneMask == null;


        public WorldQueryFilterBuilder(WorldCore world)
        {
            World = world;
            AllMask = AnyMask = NoneMask = null;
            SharedComponents = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldQueryFilterBuilder WithAll<T>()
        {
            Set(ref AllMask, WorldComponentRegistry.GetId<T>());
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldQueryFilterBuilder WithAny<T>()
        {
            Set(ref AnyMask, WorldComponentRegistry.GetId<T>());
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldQueryFilterBuilder WithNone<T>()
        {
            Set(ref NoneMask, WorldComponentRegistry.GetId<T>());
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldQueryFilterBuilder WithShared<T>(in T value) where T : IWorldSharedComponent
        {
            var hash = value.GetHashCode();
            World.Assert(hash != 0);
            World.Assert(hash != -1);
            (SharedComponents ??= new List<WorldQuerySharedComponent>())
                .Add(new WorldQuerySharedComponent(WorldComponentRegistry.GetId<T>(), hash));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Set(ref ulong[] mask, WorldIncrementId componentId)
        {
            if (mask == null || mask.Length <= BitArrayOperator.GetBitsLength(componentId.Raw))
                Array.Resize(ref mask, BitArrayOperator.GetBitsLength(componentId.Raw));
            BitArrayOperator.SetBit(mask, componentId, true);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            AllMask?.AsSpan().Clear();
            AnyMask?.AsSpan().Clear();
            NoneMask?.AsSpan().Clear();
            SharedComponents?.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly bool Match(WorldArchetype archetype)
        {
            if (AllMask != null && !BitArrayOperator.MaskAll(archetype.ComponentMask, AllMask))
                return false;
            if (AnyMask != null && !BitArrayOperator.MaskAny(archetype.ComponentMask, AnyMask))
                return false;
            return NoneMask == null || !BitArrayOperator.MaskAll(archetype.ComponentMask, NoneMask);
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly WorldQueryFilter Build()
        {
            return new WorldQueryFilter(World, this);
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly WorldQueryEnumerator Query()
        {
            return new WorldQueryEnumerator(World, this);
        }

        /// <summary>
        /// 
        /// </summary>
        public static implicit operator WorldQueryFilter(in WorldQueryFilterBuilder builder) => builder.Build();
    }
}