// ==================== qcbf@qq.com |2025-12-11 ====================

using System;
using System.Collections;
using System.Collections.Generic;

namespace FLib
{
    public struct FixedIndexList<T> : IList<T>
    {
        public T[] Values;
        public Stack<int> Frees;
#if DEBUG
        public HashSet<int> Uses;
#endif

        public int Count { get; private set; }
        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get => Values[index];
            set => Values[index] = value;
        }

        public FixedIndexList(int capacity) : this()
        {
            capacity = Math.Max(4, capacity);
            Values = new T[capacity];
            Frees = new Stack<int>(capacity >> 1);
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();


        public void EnsureCapacity(int capacity)
        {
            if (Values.Length >= capacity) return;
            Array.Resize(ref Values, MathEx.GetNextPowerOfTwo(capacity));
#if NET6_0_OR_GREATER
            Frees.EnsureCapacity(capacity >> 1);
#endif
        }

        public readonly ref T GetRef(int index) => ref Values[index];
        void ICollection<T>.Add(T item) => Add(item);

        /// <summary>
        /// 
        /// </summary>
        public int Add()
        {
            if (Frees == null || !Frees.TryPop(out var index))
            {
                if (Values == null || Values.Length <= Count)
                    Array.Resize(ref Values, MathEx.GetNextPowerOfTwo(Count + 1));
                index = Count;
            }

            ++Count;
#if DEBUG
            (Uses ??= new HashSet<int>()).Add(index);
#endif
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
#if DEBUG
            if (!(Uses ??= new HashSet<int>()).Remove(index))
                throw new Exception($"not found {index}");
#endif
            --Count;
            if (index < Count)
                (Frees ??= new Stack<int>()).Push(index);
            if (clean)
                Values[index] = default;
        }

        public void Clear() => Clear(true);

        public void Clear(bool clean)
        {
            Frees?.Clear();
            Count = 0;
            if (clean)
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
        public int IndexOf(in T item) => Array.IndexOf(Values, item);
        public bool Contains(in T item) => IndexOf(item) >= 0;
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _values;
            private readonly HashSet<int> _frees;
            private readonly int _count;
            private int _index;
            private int _found;
            public T Current { get; private set; }
            object IEnumerator.Current => Current!;

            public Enumerator(in FixedIndexList<T> source)
            {
                _values = source.Values;
                _frees = source.Frees != null ? new HashSet<int>(source.Frees) : null;
                _count = source.Count;
                _index = -1;
                _found = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                while (++_index < _values.Length && _found < _count)
                {
                    if (_frees?.Contains(_index) == true)
                        continue;
                    ++_found;
                    Current = _values[_index];
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _index = -1;
                _found = 0;
                Current = default;
            }

            public void Dispose()
            {
            }
        }
    }
}