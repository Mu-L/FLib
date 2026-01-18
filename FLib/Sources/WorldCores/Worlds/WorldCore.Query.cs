// ==================== qcbf@qq.com | 2026-01-18 ====================


using System.Collections;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    public unsafe partial class WorldCore
    {

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1> Query<T1>(in QueryFilter filter = default) where T1 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1> : IEnumerable<(Entity, Ref<T1>)> where T1 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>)> IEnumerable<(Entity, Ref<T1>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
                public (Entity, Ref<T1>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1, T2> Query<T1, T2>(in QueryFilter filter = default) where T1 : unmanaged where T2 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1, T2> : IEnumerable<(Entity, Ref<T1>, Ref<T2>)> where T1 : unmanaged where T2 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>, Ref<T2>)> IEnumerable<(Entity, Ref<T1>, Ref<T2>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>, Ref<T2>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
				private T2* _component2;
                public (Entity, Ref<T1>, Ref<T2>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index), new Ref<T2>(_component2 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
					_component2 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
					_component2 = chunk.Get<T2>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1, T2, T3> Query<T1, T2, T3>(in QueryFilter filter = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1, T2, T3> : IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>)> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>)> IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
				private T2* _component2;
				private T3* _component3;
                public (Entity, Ref<T1>, Ref<T2>, Ref<T3>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index), new Ref<T2>(_component2 + _index), new Ref<T3>(_component3 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
					_component2 = null;
					_component3 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
					_component2 = chunk.Get<T2>(0);
					_component3 = chunk.Get<T3>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1, T2, T3, T4> Query<T1, T2, T3, T4>(in QueryFilter filter = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1, T2, T3, T4> : IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>)> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>)> IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
				private T2* _component2;
				private T3* _component3;
				private T4* _component4;
                public (Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index), new Ref<T2>(_component2 + _index), new Ref<T3>(_component3 + _index), new Ref<T4>(_component4 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
					_component2 = null;
					_component3 = null;
					_component4 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
					_component2 = chunk.Get<T2>(0);
					_component3 = chunk.Get<T3>(0);
					_component4 = chunk.Get<T4>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(in QueryFilter filter = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1, T2, T3, T4, T5> : IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>)> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>)> IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
				private T2* _component2;
				private T3* _component3;
				private T4* _component4;
				private T5* _component5;
                public (Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index), new Ref<T2>(_component2 + _index), new Ref<T3>(_component3 + _index), new Ref<T4>(_component4 + _index), new Ref<T5>(_component5 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
					_component2 = null;
					_component3 = null;
					_component4 = null;
					_component5 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
					_component2 = chunk.Get<T2>(0);
					_component3 = chunk.Get<T3>(0);
					_component4 = chunk.Get<T4>(0);
					_component5 = chunk.Get<T5>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(in QueryFilter filter = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1, T2, T3, T4, T5, T6> : IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>)> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>)> IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
				private T2* _component2;
				private T3* _component3;
				private T4* _component4;
				private T5* _component5;
				private T6* _component6;
                public (Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index), new Ref<T2>(_component2 + _index), new Ref<T3>(_component3 + _index), new Ref<T4>(_component4 + _index), new Ref<T5>(_component5 + _index), new Ref<T6>(_component6 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
					_component2 = null;
					_component3 = null;
					_component4 = null;
					_component5 = null;
					_component6 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
					_component2 = chunk.Get<T2>(0);
					_component3 = chunk.Get<T3>(0);
					_component4 = chunk.Get<T4>(0);
					_component5 = chunk.Get<T5>(0);
					_component6 = chunk.Get<T6>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(in QueryFilter filter = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1, T2, T3, T4, T5, T6, T7> : IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>)> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>)> IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
				private T2* _component2;
				private T3* _component3;
				private T4* _component4;
				private T5* _component5;
				private T6* _component6;
				private T7* _component7;
                public (Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index), new Ref<T2>(_component2 + _index), new Ref<T3>(_component3 + _index), new Ref<T4>(_component4 + _index), new Ref<T5>(_component5 + _index), new Ref<T6>(_component6 + _index), new Ref<T7>(_component7 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
					_component2 = null;
					_component3 = null;
					_component4 = null;
					_component5 = null;
					_component6 = null;
					_component7 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
					_component2 = chunk.Get<T2>(0);
					_component3 = chunk.Get<T3>(0);
					_component4 = chunk.Get<T4>(0);
					_component5 = chunk.Get<T5>(0);
					_component6 = chunk.Get<T6>(0);
					_component7 = chunk.Get<T7>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryEnumerator<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(in QueryFilter filter = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public readonly struct QueryEnumerator<T1, T2, T3, T4, T5, T6, T7, T8> : IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>, Ref<T8>)> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged 
        {
            private readonly QueryFilter _filter;
            public Enumerable GetEnumerator() => new(_filter);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>, Ref<T8>)> IEnumerable<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>, Ref<T8>)>.GetEnumerator() => GetEnumerator();
            public QueryEnumerator(in WorldCore world, in QueryFilter filter)
            {
                _filter = filter;
                if (_filter.IsEmpty)
                    _filter = new QueryFilterBuilder(world).Build();
            }
            public struct Enumerable : IEnumerator<(Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>, Ref<T8>)>
            {
                public ChunkQueryEnumerator ChunkEnumerator;
                private int _index;
                private int _count;
                private Entity* _entity;
                private T1* _component1;
				private T2* _component2;
				private T3* _component3;
				private T4* _component4;
				private T5* _component5;
				private T6* _component6;
				private T7* _component7;
				private T8* _component8;
                public (Entity, Ref<T1>, Ref<T2>, Ref<T3>, Ref<T4>, Ref<T5>, Ref<T6>, Ref<T7>, Ref<T8>) Current => (*(_entity + _index), new Ref<T1>(_component1 + _index), new Ref<T2>(_component2 + _index), new Ref<T3>(_component3 + _index), new Ref<T4>(_component4 + _index), new Ref<T5>(_component5 + _index), new Ref<T6>(_component6 + _index), new Ref<T7>(_component7 + _index), new Ref<T8>(_component8 + _index));
                object IEnumerator.Current => Current;
                public Enumerable(QueryFilter filter)
                {
                    ChunkEnumerator = new ChunkQueryEnumerator(filter);
                    _count = _index = 0;
                    _entity = null;
                    _component1 = null;
					_component2 = null;
					_component3 = null;
					_component4 = null;
					_component5 = null;
					_component6 = null;
					_component7 = null;
					_component8 = null;
                }
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
                    _component1 = chunk.Get<T1>(0);
					_component2 = chunk.Get<T2>(0);
					_component3 = chunk.Get<T3>(0);
					_component4 = chunk.Get<T4>(0);
					_component5 = chunk.Get<T5>(0);
					_component6 = chunk.Get<T6>(0);
					_component7 = chunk.Get<T7>(0);
					_component8 = chunk.Get<T8>(0);
                    return true;
                }
                public void Reset() { }
                public void Dispose() => ChunkEnumerator.Dispose();
            }
        }
    }
}