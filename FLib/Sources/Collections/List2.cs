using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FLib
{
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    public class List2<T> : IList<T>, IReadOnlyList<T>
    {
        private const int DefaultCapacity = 4;
        private const int MaxArrayLength = 0X7FEFFFFF;
        private static readonly T[] EmptyArray = Array.Empty<T>();

        private T[] _items;
        private int _version;

        public List2()
        {
            _items = EmptyArray;
        }

        public List2(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _items = capacity == 0 ? EmptyArray : new T[capacity];
        }

        public List2(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> items)
            {
                var count = items.Count;
                if (count == 0)
                {
                    _items = EmptyArray;
                    return;
                }

                _items = new T[count];
                items.CopyTo(_items, 0);
                Count = count;
                return;
            }

            _items = EmptyArray;
            AddRange(collection);
        }

        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < Count) throw new ArgumentOutOfRangeException(nameof(value));
                if (value == _items.Length) return;
                _items = value == 0 ? EmptyArray : Resize(value);
            }
        }

        public int Count { get; private set; }

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                CheckIndex(index);
                return _items[index];
            }
            set
            {
                CheckIndex(index);
                _items[index] = value;
                _version++;
            }
        }

        /// <summary> Returns a reference to the element at <paramref name="index"/>. </summary>
        /// <remarks>Do not retain the reference across structural modifications that can reallocate the internal storage.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetValueRef(int index)
        {
            CheckIndex(index);
            return ref _items[index];
        }

        public void Add(T item)
        {
            if (Count == _items.Length) Grow(Count + 1);
            _items[Count++] = item;
            _version++;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(Count, collection);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            CheckRange(index, count);
            return Array.BinarySearch(_items, index, count, item, comparer);
        }

        public void Clear()
        {
            if (Count > 0)
                Array.Clear(_items, 0, Count);
            Count = 0;
            _version++;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public List2<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new List2<TOutput>(Count);
            for (var i = 0; i < Count; i++)
                result._items[i] = converter(_items[i]);
            result.Count = Count;
            return result;
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, Count);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            CheckRange(index, count);
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (_items.Length < capacity) Grow(capacity);
            return _items.Length;
        }

        public bool Exists(Predicate<T> match)
        {
            return FindIndex(match) >= 0;
        }

        public T Find(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            for (var i = 0; i < Count; i++)
            {
                if (match(_items[i])) return _items[i];
            }

            return default;
        }

        public List2<T> FindAll(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            var result = new List2<T>();
            for (var i = 0; i < Count; i++)
            {
                if (match(_items[i])) result.Add(_items[i]);
            }

            return result;
        }

        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, Count, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, Count - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            CheckRange(startIndex, count);
            if (match == null) throw new ArgumentNullException(nameof(match));

            var endIndex = startIndex + count;
            for (var i = startIndex; i < endIndex; i++)
            {
                if (match(_items[i])) return i;
            }

            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            for (var i = Count - 1; i >= 0; i--)
            {
                if (match(_items[i])) return _items[i];
            }

            return default;
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(Count - 1, Count, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (Count == 0 && startIndex == -1) return -1;
            if ((uint)startIndex >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || count > startIndex + 1) throw new ArgumentOutOfRangeException(nameof(count));

            var endIndex = startIndex - count;
            for (var i = startIndex; i > endIndex; i--)
            {
                if (match(_items[i])) return i;
            }

            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var version = _version;
            for (var i = 0; i < Count; i++)
            {
                if (version != _version) break;
                action(_items[i]);
            }

            if (version != _version) throw new InvalidOperationException("Collection was modified during enumeration.");
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public List2<T> GetRange(int index, int count)
        {
            CheckRange(index, count);
            var result = new List2<T>(count);
            Array.Copy(_items, index, result._items, 0, count);
            result.Count = count;
            return result;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, Count);
        }

        public int IndexOf(T item, int index)
        {
            if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Array.IndexOf(_items, item, index, Count - index);
        }

        public int IndexOf(T item, int index, int count)
        {
            CheckRange(index, count);
            return Array.IndexOf(_items, item, index, count);
        }

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (Count == _items.Length) Grow(Count + 1);
            if (index < Count) Array.Copy(_items, index, _items, index + 1, Count - index);
            _items[index] = item;
            Count++;
            _version++;
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> items)
            {
                var count = items.Count;
                if (count == 0) return;
                EnsureCapacity(Count + count);
                if (index < Count) Array.Copy(_items, index, _items, index + count, Count - index);

                if (ReferenceEquals(this, items))
                {
                    Array.Copy(_items, 0, _items, index, index);
                    Array.Copy(_items, index + count, _items, index * 2, Count - index);
                }
                else
                {
                    items.CopyTo(_items, index);
                }

                Count += count;
                _version++;
                return;
            }

            foreach (var item in collection)
                Insert(index++, item);
        }

        public int LastIndexOf(T item)
        {
            return Count == 0 ? -1 : LastIndexOf(item, Count - 1, Count);
        }

        public int LastIndexOf(T item, int index)
        {
            if (Count == 0) return -1;
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return LastIndexOf(item, index, index + 1);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            if (Count == 0) return -1;
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > index + 1) throw new ArgumentOutOfRangeException(nameof(count));
            return Array.LastIndexOf(_items, item, index, count);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public int RemoveAll(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var freeIndex = 0;
            while (freeIndex < Count && !match(_items[freeIndex])) freeIndex++;
            if (freeIndex >= Count) return 0;

            var current = freeIndex + 1;
            while (current < Count)
            {
                while (current < Count && match(_items[current])) current++;
                if (current < Count) _items[freeIndex++] = _items[current++];
            }

            Array.Clear(_items, freeIndex, Count - freeIndex);
            var result = Count - freeIndex;
            Count = freeIndex;
            _version++;
            return result;
        }

        public void RemoveAt(int index)
        {
            CheckIndex(index);
            Count--;
            if (index < Count) Array.Copy(_items, index + 1, _items, index, Count - index);
            _items[Count] = default;
            _version++;
        }

        public void RemoveRange(int index, int count)
        {
            CheckRange(index, count);
            if (count == 0) return;

            Count -= count;
            if (index < Count) Array.Copy(_items, index + count, _items, index, Count - index);
            Array.Clear(_items, Count, count);
            _version++;
        }

        public void Reverse()
        {
            Reverse(0, Count);
        }

        public void Reverse(int index, int count)
        {
            CheckRange(index, count);
            Array.Reverse(_items, index, count);
            _version++;
        }

        public void Sort()
        {
            Sort(0, Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            CheckRange(index, count);
            if (count > 1) Array.Sort(_items, index, count, comparer);
            _version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            if (Count > 1) Array.Sort(_items, 0, Count, Comparer<T>.Create(comparison));
            _version++;
        }

        public T[] ToArray()
        {
            if (Count == 0) return EmptyArray;
            var result = new T[Count];
            Array.Copy(_items, result, Count);
            return result;
        }

        public void TrimExcess()
        {
            if (Count < (int)(_items.Length * 0.9)) Capacity = Count;
        }

        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            for (var i = 0; i < Count; i++)
            {
                if (!match(_items[i])) return false;
            }

            return true;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIndex(int index)
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRange(int index, int count)
        {
            if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > Count - index) throw new ArgumentOutOfRangeException(nameof(count));
        }

        private T[] Resize(int capacity)
        {
            var items = new T[capacity];
            if (Count > 0) Array.Copy(_items, items, Count);
            return items;
        }

        private void Grow(int capacity)
        {
            var newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
            if ((uint)newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
            if (newCapacity < capacity) newCapacity = capacity;
            Capacity = newCapacity;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly List2<T> _list;
            private readonly int _version;
            private int _index;

            internal Enumerator(List2<T> list)
            {
                _list = list;
                _version = list._version;
                _index = 0;
                Current = default;
            }

            public T Current { get; private set; }

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list.Count + 1)
                        throw new InvalidOperationException("Enumeration has not started or has already finished.");
                    return Current;
                }
            }

            public bool MoveNext()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("Collection was modified during enumeration.");
                if ((uint)_index < (uint)_list.Count)
                {
                    Current = _list._items[_index++];
                    return true;
                }

                _index = _list.Count + 1;
                Current = default;
                return false;
            }

            public void Reset()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("Collection was modified during enumeration.");
                _index = 0;
                Current = default;
            }

            public void Dispose()
            {
            }
        }
    }
}
