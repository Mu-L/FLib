// ==================== qcbf@qq.com | 2026-01-17 ====================

using System.Collections;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct WorldQueryEnumerator : IEnumerable<WorldEntity>
    {
        public readonly WorldQueryFilter Filter;
        public Enumerable GetEnumerator() => new(Filter);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<WorldEntity> IEnumerable<WorldEntity>.GetEnumerator() => GetEnumerator();

        public WorldQueryEnumerator(in WorldCore world, in WorldQueryFilter filter)
        {
            Filter = filter;
            if (Filter.IsEmpty)
                Filter = new WorldQueryFilterBuilder(world).Build();
        }

        /// <summary>
        /// 
        /// </summary>
        public unsafe struct Enumerable : IEnumerator<WorldEntity>
        {
            public WorldChunkQueryEnumerator ChunkEnumerator;
            private int _index;
            private int _count;
            private WorldEntity* _entity;
            public WorldEntity Current => *(_entity + _index);
            object IEnumerator.Current => Current;

            public Enumerable(WorldQueryFilter filter)
            {
                ChunkEnumerator = new WorldChunkQueryEnumerator(filter);
                _count = _index = 0;
                _entity = null;
            }

            /// <summary>
            /// 
            /// </summary>
            public bool MoveNext()
            {
                if (++_index < _count)
                    return true;
                if (!ChunkEnumerator.MoveNext())
                    return false;

                var chunk = ChunkEnumerator.Current!;
                _index = 0;
                _count = chunk.Count;
                _entity = chunk.GetEntity(0);
                return true;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
                ChunkEnumerator.Dispose();
            }
        }
    }
}