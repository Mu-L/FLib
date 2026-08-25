// ==================== qcbf@qq.com |2025-12-11 ====================

using System;
using System.Collections;
using System.Collections.Generic;

namespace FLib
{
    /// <summary> 稳定索引列表 </summary>
    public struct StableIndexList<T> : IList<T>
    {
        public T[] Values;
        public StableIndexAllocator IndexAllocator;
        public int Count => IndexAllocator.Count;
        public int EndCount => IndexAllocator.EndCount;
        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get => Values[index];
            set => Values[index] = value;
        }

        public StableIndexList(int capacity) : this()
        {
            capacity = Math.Max(4, capacity);
            Values = new T[capacity];
            IndexAllocator.Frees = new Stack<int>(Math.Max(8, capacity >> 1));
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public void EnsureCapacity(int capacity)
        {
            if (Values != null && Values.Length >= capacity) return;
            Array.Resize(ref Values, capacity);
        }

        public readonly ref T GetRef(int index) => ref Values[index];
        void ICollection<T>.Add(T item) => Add(item);

        /// <summary>
        /// 
        /// </summary>
        public int Add()
        {
            var index = IndexAllocator.Alloc();
            if (Values == null || Values.Length <= index)
                Array.Resize(ref Values, MathEx.GetNextCapacityLength(Count));
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Add(in T item)
        {
            var idx = Add();
            Values[idx] = item;
            return idx;
        }

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        public void RemoveAt(int index) => RemoveAt(index, true);

        public void RemoveAt(int index, bool clean)
        {
            IndexAllocator.Free(index);
            if (clean)
                Values[index] = default;
        }

        public void Clear() => Clear(true);

        public void Clear(bool clean)
        {
            IndexAllocator.Clear();
            if (clean && Values != null)
                Array.Fill(Values, default);
        }

        bool ICollection<T>.Contains(T item) => Contains(item);

        bool ICollection<T>.Remove(T item) => Remove(item);

        public bool Remove(in T item)
        {
            var index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        int IList<T>.IndexOf(T item) => IndexOf(item);
        public int IndexOf(in T item) => Count == 0 ? -1 : Array.IndexOf(Values, item, 0, IndexAllocator.EndCount);
        public bool Contains(in T item) => IndexOf(item) >= 0;
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _values;
            private readonly HashSet<int> _frees;
            private int _index;
            private readonly int _count;
            public T Current => _values[_index];
            object IEnumerator.Current => Current!;

            public Enumerator(in StableIndexList<T> source)
            {
                _values = source.Values;
                _frees = source.IndexAllocator.Frees != null ? new HashSet<int>(source.IndexAllocator.Frees) : null;
                _count = source.IndexAllocator.EndCount;
                _index = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _count)
                {
                    if (_frees?.Contains(_index) != true)
                        return true;
                }

                return false;
            }

            public void Reset()
            {
                _index = -1;
            }

            void IDisposable.Dispose()
            {
            }
        }
    }

    /// <summary> 稳定的索引分配器 </summary>
    public struct StableIndexAllocator : IEnumerable<int>
    {
#if DEBUG
        public HashSet<int> Uses;
#endif
        public Stack<int> Frees;
        public int EndCount;
        public int Count;

        /// <summary> 分配索引 </summary>
        public int Alloc()
        {
            if (Frees?.TryPop(out var index) == true)
            {
                ++Count;
            }
            else
            {
                ++Count;
                index = EndCount++;
            }
#if DEBUG
            if (!(Uses ??= new HashSet<int>()).Add(index))
                throw new Exception($"alloc {index} error");
#endif
            return index;
        }

        /// <summary> 释放索引 </summary>
        public void Free(int index, bool cleanIndex = true)
        {
            System.Diagnostics.Debug.Assert(Count > 0 && index < EndCount && index >= 0);
            --Count;
            if (index == EndCount - 1)
            {
                --EndCount;
                if (cleanIndex && Frees != null)
                {
                    while (EndCount > 0 && Frees.Count > 0 && Frees.Peek() == EndCount - 1)
                    {
                        Frees.Pop();
                        --EndCount;
                    }
                }
            }
            else
            {
                (Frees ??= new Stack<int>()).Push(index);
            }
#if DEBUG
            if (!(Uses ??= new HashSet<int>()).Remove(index))
                throw new Exception($"free {index} error");
#endif
        }

        /// <summary> 清空索引分配器 </summary>
        public void Clear()
        {
            Count = EndCount = 0;
            Frees?.Clear();
#if DEBUG
            Uses?.Clear();
#endif
        }

        public static implicit operator Stack<int>(in StableIndexAllocator allocator) => allocator.Frees;
        public static implicit operator int(in StableIndexAllocator allocator) => allocator.Count;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<int> GetEnumerator() => Frees.GetEnumerator();
    }
}
