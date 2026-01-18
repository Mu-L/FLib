// ==================== qcbf@qq.com | 2026-01-09 ====================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public struct QueryFilterBuilder
    {
        public readonly WorldCore World;
        internal ulong[] AllMask;
        internal ulong[] AnyMask;
        internal ulong[] NoneMask;
        public List<QuerySharedComponent> SharedComponents;

        public bool IsEmpty => AllMask == null && AnyMask == null && NoneMask == null;


        public QueryFilterBuilder(WorldCore world)
        {
            World = world;
            AllMask = AnyMask = NoneMask = null;
            SharedComponents = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryFilterBuilder All<T>()
        {
            Set(ref AllMask, ComponentRegistry.GetId<T>());
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryFilterBuilder Any<T>()
        {
            Set(ref AnyMask, ComponentRegistry.GetId<T>());
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryFilterBuilder None<T>()
        {
            Set(ref NoneMask, ComponentRegistry.GetId<T>());
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryFilterBuilder SharedComponent<T>(in T value) where T : ISharedComponent
        {
            var hash = value.GetHashCode();
            Debug.Assert(hash != 0);
            Debug.Assert(hash != -1);
            (SharedComponents ??= new List<QuerySharedComponent>())
                .Add(new QuerySharedComponent(ComponentRegistry.GetId<T>(), hash));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Set(ref ulong[] mask, IncrementId componentId)
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
        public readonly bool Match(Archetype archetype)
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
        public readonly QueryFilter Build()
        {
            return new QueryFilter(World, this);
        }

        /// <summary>
        /// 
        /// </summary>
        public static implicit operator QueryFilter(in QueryFilterBuilder builder) => builder.Build();
    }
}