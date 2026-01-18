// ==================== qcbf@qq.com | 2026-01-17 ====================

using System.Collections;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct QueryEnumerator : IEnumerable<Entity>
    {
        private readonly QueryFilter _filter;
        public Enumerable GetEnumerator() => new(_filter);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();

        public QueryEnumerator(in WorldCore world, in QueryFilter filter)
        {
            _filter = filter;
            if (_filter.IsEmpty)
                _filter = new QueryFilterBuilder(world).Build();
        }

        /// <summary>
        /// 
        /// </summary>
        public unsafe struct Enumerable : IEnumerator<Entity>
        {
            public ChunkQueryEnumerator ChunkEnumerator;
            private int _index;
            private int _count;
            private Entity* _entity;
            public Entity Current => *(_entity + _index);
            object IEnumerator.Current => Current;

            public Enumerable(QueryFilter filter)
            {
                ChunkEnumerator = new ChunkQueryEnumerator(filter);
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