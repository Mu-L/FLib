using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FLib
{
    [Serializable]
    [DebuggerTypeProxy(typeof(HashSet2DebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class HashSet2<T> : ISet<T>
    {
        private const int StartOfFreeList = -3;

        private int[] _buckets;
        private Entry[] _entries;
        private int _count;
        private int _freeList = -1;
        private int _freeCount;
        private readonly IEqualityComparer<T> _comparer;

        public HashSet2() : this(0)
        {
        }

        public HashSet2(IEqualityComparer<T> comparer) : this(0, comparer)
        {
        }

        public HashSet2(int capacity, IEqualityComparer<T> comparer = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _comparer = comparer ?? EqualityComparer<T>.Default;
            if (capacity > 0)
                Initialize(capacity);
        }

        public HashSet2(IEnumerable<T> collection, IEqualityComparer<T> comparer = null)
            : this((collection as ICollection<T>)?.Count ?? 0, comparer)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
                Add(in item);
        }

        public HashSet2(HashSet2<T> set, IEqualityComparer<T> comparer = null)
            : this(set?.Count ?? 0, comparer ?? set?._comparer)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            foreach (var item in set)
                Add(in item);
        }

        [Serializable, DebuggerDisplay("({Value})->{Next}")]
        public struct Entry
        {
            public int HashCode;
            public int Next;
            public T Value;
        }

        public int Count => _count - _freeCount;

        public int Capacity => _entries?.Length ?? 0;

        public IEqualityComparer<T> Comparer => _comparer;

        public bool Contains(in T item)
        {
            return FindItemIndex(in item, out _) >= 0;
        }

        public bool TryGetValue(in T equalValue, out T actualValue)
        {
            var index = FindItemIndex(in equalValue, out _);
            if (index >= 0)
            {
                actualValue = _entries[index].Value;
                return true;
            }

            actualValue = default;
            return false;
        }

        public ref readonly T GetValueRef(in T item)
        {
            var index = FindItemIndex(in item, out _);
            if (index < 0)
                throw new KeyNotFoundException($"The given value '{item}' was not present in the set.");

            return ref _entries[index].Value;
        }

        public ref readonly T GetValueRefOrNullRef(in T item)
        {
            var index = FindItemIndex(in item, out _);
            return ref index >= 0 ? ref _entries[index].Value : ref Unsafe.NullRef<T>();
        }

        public ref readonly T GetValueOrAdd(in T item)
        {
            var hashCode = GetHashCode(item);
            if (_buckets == null)
                Initialize(0);

            var index = FindItemIndex(in item, hashCode, out var bucketIndex);
            if (index >= 0)
                return ref _entries[index].Value;

            return ref AddEntry(item, hashCode, bucketIndex);
        }

        public bool Add(in T item)
        {
            var hashCode = GetHashCode(item);
            if (_buckets == null)
                Initialize(0);

            if (FindItemIndex(in item, hashCode, out var bucketIndex) >= 0)
                return false;

            AddEntry(item, hashCode, bucketIndex);
            return true;
        }

        public bool TryAdd(in T item)
        {
            return Add(in item);
        }

        public bool Remove(in T item)
        {
            return Remove(in item, out _);
        }

        public bool Remove(in T item, out T actualValue)
        {
            actualValue = default;
            if (_buckets == null)
                return false;

            var hashCode = GetHashCode(item);
            var bucketIndex = GetBucketIndex(hashCode, _buckets.Length);
            var index = _buckets[bucketIndex] - 1;
            var lastIndex = -1;
            var collisionCount = 0;
            while ((uint)index < (uint)_entries.Length)
            {
                ref var entry = ref _entries[index];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Value, item))
                {
                    actualValue = entry.Value;
                    RemoveEntryFromChain(bucketIndex, lastIndex, index, entry.Next);
                    return true;
                }

                lastIndex = index;
                index = entry.Next;
                if (collisionCount++ >= _entries.Length)
                    ThrowConcurrentOperation();
            }

            return false;
        }

        public void Clear()
        {
            if (_buckets == null)
                return;

            Array.Clear(_buckets, 0, _buckets.Length);
            if (_count > 0)
                Array.Clear(_entries, 0, _count);
            _count = 0;
            _freeList = -1;
            _freeCount = 0;
        }

        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            var currentCapacity = _entries?.Length ?? 0;
            if (currentCapacity >= capacity)
                return currentCapacity;

            if (_buckets == null)
                return Initialize(capacity);

            var newSize = HashHelpers2.GetPrime(capacity);
            Resize(newSize);
            return newSize;
        }

        public void TryAddCapacity(int addCapacity)
        {
            EnsureCapacity(checked(Count + addCapacity));
        }

        public void ResetToCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            Clear();
            if (capacity == 0)
            {
                _buckets = null;
                _entries = null;
                return;
            }

            Initialize(capacity);
        }

        public void TrimExcess()
        {
            TrimExcess(Count);
        }

        public void TrimExcess(int capacity)
        {
            if (capacity < Count)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            var currentCapacity = _entries?.Length ?? 0;
            var newSize = HashHelpers2.GetPrime(capacity);
            if (newSize >= currentCapacity)
                return;

            Resize(newSize);
        }

        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var removedCount = 0;
            using var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (match(enumerator.Current))
                {
                    enumerator.RemoveSelf();
                    removedCount++;
                }
            }

            return removedCount;
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return;

            foreach (var item in other)
                Add(in item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
            {
                Clear();
                return;
            }

            foreach (var item in other)
                Remove(in item);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return;

            var otherSet = CreateComparableSet(other);
            using var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (!otherSet.Contains(in item))
                    enumerator.RemoveSelf();
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
            {
                Clear();
                return;
            }

            var otherSet = CreateComparableSet(other);
            foreach (var item in otherSet)
            {
                if (!Remove(in item))
                    Add(in item);
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return true;

            var otherSet = CreateComparableSet(other);
            if (Count > otherSet.Count)
                return false;
            foreach (var item in this)
            {
                if (!otherSet.Contains(in item))
                    return false;
            }

            return true;
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return false;

            var otherSet = CreateComparableSet(other);
            return Count < otherSet.Count && IsSubsetOf(otherSet);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            foreach (var item in other)
            {
                if (!Contains(in item))
                    return false;
            }

            return true;
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return false;

            var otherSet = CreateComparableSet(other);
            return Count > otherSet.Count && IsSupersetOf(otherSet);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            foreach (var item in other)
            {
                if (Contains(in item))
                    return true;
            }

            return false;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return true;

            var otherSet = CreateComparableSet(other);
            return Count == otherSet.Count && IsSubsetOf(otherSet);
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0, Count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, Count);
        }

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (count < 0 || (uint)arrayIndex > (uint)array.Length || array.Length - arrayIndex < count)
                throw new ArgumentException("The destination array is too small.", nameof(array));
            if (count > Count)
                throw new ArgumentException("The requested number of elements is greater than the set size.", nameof(count));

            var copied = 0;
            for (var i = 0; i < _count && copied < count; i++)
            {
                if (_entries[i].Next >= -1)
                    array[arrayIndex + copied++] = _entries[i].Value;
            }
        }

        public T[] ToArray()
        {
            var result = new T[Count];
            CopyTo(result);
            return result;
        }

        public Enumerator GetEnumerator() => new(this);

        private HashSet2<T> CreateComparableSet(IEnumerable<T> other)
        {
            if (other is HashSet2<T> otherSet && Equals(_comparer, otherSet._comparer))
                return otherSet;
            return new HashSet2<T>(other, _comparer);
        }

        private int Initialize(int capacity)
        {
            var size = HashHelpers2.GetPrime(capacity);
            var buckets = new int[size];
            var entries = new Entry[size];
            _freeList = -1;
            _buckets = buckets;
            _entries = entries;
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(T item)
        {
            return item is null ? 0 : _comparer.GetHashCode(item) & 0x7FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucketIndex(int hashCode, int bucketCount)
        {
            return (int)((uint)hashCode % (uint)bucketCount);
        }

        private int FindItemIndex(in T item, out int bucketIndex)
        {
            return FindItemIndex(in item, GetHashCode(item), out bucketIndex);
        }

        private int FindItemIndex(in T item, int hashCode, out int bucketIndex)
        {
            if (_buckets == null)
            {
                bucketIndex = -1;
                return -1;
            }

            bucketIndex = GetBucketIndex(hashCode, _buckets.Length);
            var index = _buckets[bucketIndex] - 1;
            var collisionCount = 0;
            while ((uint)index < (uint)_entries.Length)
            {
                ref var entry = ref _entries[index];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Value, item))
                    return index;

                index = entry.Next;
                if (collisionCount++ >= _entries.Length)
                    ThrowConcurrentOperation();
            }

            return -1;
        }

        private ref T AddEntry(T item, int hashCode, int bucketIndex)
        {
            var entries = _entries;
            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                _freeList = StartOfFreeList - entries[index].Next;
                _freeCount--;
            }
            else
            {
                var count = _count;
                if (count == entries.Length)
                {
                    Resize();
                    entries = _entries;
                    bucketIndex = GetBucketIndex(hashCode, _buckets.Length);
                }

                index = count;
                _count = count + 1;
            }

            ref var entry = ref entries[index];
            entry.HashCode = hashCode;
            entry.Next = _buckets[bucketIndex] - 1;
            entry.Value = item;
            _buckets[bucketIndex] = index + 1;
            return ref entry.Value;
        }

        private void RemoveEntryFromChain(int bucketIndex, int lastIndex, int index, int nextIndex)
        {
            if (lastIndex < 0)
                _buckets[bucketIndex] = nextIndex + 1;
            else
                _entries[lastIndex].Next = nextIndex;

            ref var entry = ref _entries[index];
            entry.Next = StartOfFreeList - _freeList;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                entry.Value = default;
            _freeList = index;
            _freeCount++;
        }

        private bool RemoveEntryAt(int index)
        {
            if (_buckets == null || (uint)index >= (uint)_count || _entries[index].Next < -1)
                return false;

            var entry = _entries[index];
            var bucketIndex = GetBucketIndex(entry.HashCode, _buckets.Length);
            var chainIndex = _buckets[bucketIndex] - 1;
            var lastIndex = -1;
            var collisionCount = 0;
            while ((uint)chainIndex < (uint)_entries.Length)
            {
                if (chainIndex == index)
                {
                    RemoveEntryFromChain(bucketIndex, lastIndex, index, entry.Next);
                    return true;
                }

                lastIndex = chainIndex;
                chainIndex = _entries[chainIndex].Next;
                if (collisionCount++ >= _entries.Length)
                    ThrowConcurrentOperation();
            }

            throw new InvalidOperationException("The set entry chain is corrupted.");
        }

        private void Resize()
        {
            Resize(HashHelpers2.ExpandPrime(_entries.Length));
        }

        private void Resize(int newSize)
        {
            if (newSize < Count)
                newSize = HashHelpers2.GetPrime(Count);

            var oldEntries = _entries;
            var newEntries = new Entry[newSize];
            var newBuckets = new int[newSize];
            var newCount = 0;
            for (var i = 0; i < _count; i++)
            {
                ref var oldEntry = ref oldEntries[i];
                if (oldEntry.Next < -1)
                    continue;

                ref var newEntry = ref newEntries[newCount++];
                newEntry.HashCode = oldEntry.HashCode;
                newEntry.Value = oldEntry.Value;
                var bucketIndex = GetBucketIndex(newEntry.HashCode, newBuckets.Length);
                newEntry.Next = newBuckets[bucketIndex] - 1;
                newBuckets[bucketIndex] = newCount;
            }

            _buckets = newBuckets;
            _entries = newEntries;
            _count = newCount;
            _freeList = -1;
            _freeCount = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowConcurrentOperation()
        {
            throw new InvalidOperationException("Concurrent operations are not supported.");
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly HashSet2<T> _set;
            private readonly Entry[] _entries;
            private int _index;
            private int _currentIndex;

            internal Enumerator(HashSet2<T> set)
            {
                _set = set;
                _entries = set._entries;
                _index = -1;
                _currentIndex = -1;
            }

            public readonly T Current
            {
                get
                {
                    CheckCurrent();
                    return _entries[_currentIndex].Value;
                }
            }

            public readonly ref T Value
            {
                get
                {
                    CheckCurrent();
                    return ref _entries[_currentIndex].Value;
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                CheckStorage();
                while (++_index < _set._count)
                {
                    if (_entries[_index].Next >= -1)
                    {
                        _currentIndex = _index;
                        return true;
                    }
                }

                _currentIndex = -1;
                return false;
            }

            public void RemoveSelf()
            {
                CheckStorage();
                CheckCurrent();
                _set.RemoveEntryAt(_currentIndex);
                _currentIndex = -1;
            }

            public void Reset()
            {
                CheckStorage();
                _index = -1;
                _currentIndex = -1;
            }

            public void Dispose()
            {
            }

            private readonly void CheckStorage()
            {
                if (!ReferenceEquals(_entries, _set._entries))
                    throw new InvalidOperationException("Collection was resized during enumeration.");
            }

            private readonly void CheckCurrent()
            {
                CheckStorage();
                if ((uint)_currentIndex >= (uint)_set._count || _entries[_currentIndex].Next < -1)
                    throw new InvalidOperationException("Enumeration has not started or has already finished.");
            }
        }

        #region ISet and collection interfaces

        bool ICollection<T>.IsReadOnly => false;
        void ICollection<T>.Add(T item) => Add(in item);
        bool ISet<T>.Add(T item) => Add(in item);
        bool ICollection<T>.Contains(T item) => Contains(in item);
        bool ICollection<T>.Remove(T item) => Remove(in item);
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class HashSet2DebugView<T>
    {
        private readonly HashSet2<T> _set;

        public HashSet2DebugView(HashSet2<T> set)
        {
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] public T[] Items => _set.ToArray();
    }
}