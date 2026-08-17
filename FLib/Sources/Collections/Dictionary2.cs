using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FLib
{
    [Serializable]
    [DebuggerTypeProxy(typeof(Dictionary2DebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class Dictionary2<TKey, TValue> : IDictionary, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        private const int StartOfFreeList = -3;

        private int[] _buckets;
        private Entry[] _entries;
        private int _count;
        private int _freeList = -1;
        private int _freeCount;
        private readonly IEqualityComparer<TKey> _comparer;

        public Dictionary2() : this(0)
        {
        }

        public Dictionary2(IEqualityComparer<TKey> comparer) : this(0, comparer)
        {
        }

        public Dictionary2(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            if (capacity > 0)
                Initialize(capacity);
        }

        public Dictionary2(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer = null)
            : this(dictionary?.Count ?? 0, comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var item in dictionary)
                Add(item.Key, item.Value);
        }

        public Dictionary2(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer = null)
            : this((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0, comparer)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
                Add(item.Key, item.Value);
        }

        public Dictionary2(Dictionary2<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer = null)
            : this(dictionary?.Count ?? 0, comparer ?? dictionary?._comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var item in dictionary)
                Add(item.Key, item.Value);
        }

        [Serializable, DebuggerDisplay("({Key}, {Value})->{Next}")]
        public struct Entry
        {
            public int HashCode;
            public int Next;
            public TKey Key;
            public TValue Value;
        }

        public int Count => _count - _freeCount;

        public int Capacity => _entries?.Length ?? 0;

        public IEqualityComparer<TKey> Comparer => _comparer;

        public ref TValue this[TKey key] => ref GetValueRef(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetValueRef(TKey key)
        {
            var index = FindEntry(key, GetHashCode(key), out _);
            if (index < 0)
                throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");

            return ref _entries[index].Value;
        }

        public bool ContainsKey(in TKey key)
        {
            return FindEntry(in key, GetHashCode(key), out _) >= 0;
        }

        public bool TryGetValue(in TKey key, out TValue value)
        {
            var index = FindEntry(in key, GetHashCode(key), out _);
            if (index >= 0)
            {
                value = _entries[index].Value;
                return true;
            }

            value = default;
            return false;
        }

        public TValue GetValueOrDefault(TKey key)
        {
            return GetValueOrDefault(key, default);
        }

        public TValue GetValueOrDefault(TKey key, TValue defaultValue)
        {
            var index = FindEntry(key, GetHashCode(key), out _);
            return index >= 0 ? _entries[index].Value : defaultValue;
        }

        public ref TValue GetValueRefOrNullRef(TKey key)
        {
            var index = FindEntry(key, GetHashCode(key), out _);
            return ref index >= 0 ? ref _entries[index].Value : ref Unsafe.NullRef<TValue>();
        }

        public ref TValue GetValueRefOrNullRef(TKey key, out bool isNullValue)
        {
            ref var value = ref GetValueRefOrNullRef(key);
            isNullValue = Unsafe.IsNullRef(ref value);
            return ref value;
        }

        public ref TValue GetValueOrAdd(TKey key)
        {
            var hashCode = GetHashCode(key);
            if (_buckets == null)
                Initialize(0);

            var index = FindEntry(key, hashCode, out var bucketIndex);
            if (index >= 0)
                return ref _entries[index].Value;

            return ref AddEntry(key, hashCode, bucketIndex);
        }

        public ref TValue GetValueRefOrAddDefault(TKey key, out bool exists)
        {
            var hashCode = GetHashCode(key);
            if (_buckets == null)
                Initialize(0);

            var index = FindEntry(key, hashCode, out var bucketIndex);
            if (index >= 0)
            {
                exists = true;
                return ref _entries[index].Value;
            }

            exists = false;
            return ref AddEntry(key, hashCode, bucketIndex);
        }

        public void Add(TKey key, in TValue value)
        {
            TryInsert(key, in value, false, true);
        }

        public bool TryAdd(TKey key, in TValue value)
        {
            return TryInsert(key, in value, false, false);
        }

        public bool Remove(in TKey key)
        {
            return Remove(key, out _);
        }

        public bool Remove(TKey key, out TValue value)
        {
            value = default;
            if (_buckets == null)
                return false;

            var hashCode = GetHashCode(key);
            var bucketIndex = GetBucketIndex(hashCode, _buckets.Length);
            var entries = _entries;
            var index = _buckets[bucketIndex] - 1;
            var lastIndex = -1;
            var collisionCount = 0;

            while ((uint)index < (uint)entries.Length)
            {
                ref var entry = ref entries[index];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    RemoveEntryFromChain(bucketIndex, lastIndex, index, entry.Next);
                    return true;
                }

                lastIndex = index;
                index = entry.Next;
                if (collisionCount++ >= entries.Length)
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

        public ref TValue GetFirstValue()
        {
            for (var i = 0; i < _count; i++)
            {
                if (_entries[i].Next >= -1)
                    return ref _entries[i].Value;
            }

            throw new IndexOutOfRangeException();
        }

        public TKey GetFirstKey()
        {
            for (var i = 0; i < _count; i++)
            {
                if (_entries[i].Next >= -1)
                    return _entries[i].Key;
            }

            throw new IndexOutOfRangeException();
        }

        public bool ChangeKey(in TKey oldKey, in TKey newKey, bool isOverride = false)
        {
            var oldIndex = FindEntry(in oldKey, GetHashCode(oldKey), out _);
            if (oldIndex < 0)
                return false;

            var newHashCode = GetHashCode(newKey);
            var newIndex = FindEntry(in newKey, newHashCode, out var newBucketIndex);
            if (newIndex < 0)
            {
                var value = _entries[oldIndex].Value;
                ref var newValue = ref AddEntry(newKey, newHashCode, newBucketIndex);
                newValue = value;
                Remove(in oldKey);
                return true;
            }

            if (!isOverride)
                return false;

            _entries[newIndex].Value = _entries[oldIndex].Value;
            return true;
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<KeyValuePair<TKey, TValue>>();

            var result = new KeyValuePair<TKey, TValue>[Count];
            var resultIndex = 0;
            for (var i = 0; i < _count; i++)
            {
                if (_entries[i].Next >= -1)
                    result[resultIndex++] = new KeyValuePair<TKey, TValue>(_entries[i].Key, _entries[i].Value);
            }

            return result;
        }

        public ICollection<TKey> Keys
        {
            get
            {
                var keys = new TKey[Count];
                var index = 0;
                for (var i = 0; i < _count; i++)
                {
                    if (_entries[i].Next >= -1)
                        keys[index++] = _entries[i].Key;
                }

                return keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                var values = new TValue[Count];
                var index = 0;
                for (var i = 0; i < _count; i++)
                {
                    if (_entries[i].Next >= -1)
                        values[index++] = _entries[i].Value;
                }

                return values;
            }
        }

        public Enumerator GetEnumerator() => new(this);

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
        private int GetHashCode(TKey key)
        {
            return key is null ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucketIndex(int hashCode, int bucketCount)
        {
            return (int)((uint)hashCode % (uint)bucketCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(TKey key, int hashCode, out int bucketIndex)
        {
            return FindEntry(in key, hashCode, out bucketIndex);
        }

        private int FindEntry(in TKey key, int hashCode, out int bucketIndex)
        {
            if (_buckets == null)
            {
                bucketIndex = -1;
                return -1;
            }

            bucketIndex = GetBucketIndex(hashCode, _buckets.Length);
            var entries = _entries;
            var index = _buckets[bucketIndex] - 1;
            var collisionCount = 0;
            while ((uint)index < (uint)entries.Length)
            {
                ref var entry = ref entries[index];
                if (entry.HashCode == hashCode && _comparer.Equals(entry.Key, key))
                    return index;

                index = entry.Next;
                if (collisionCount++ >= entries.Length)
                    ThrowConcurrentOperation();
            }

            return -1;
        }

        private bool TryInsert(TKey key, in TValue value, bool overwriteExisting, bool throwOnExisting)
        {
            var hashCode = GetHashCode(key);
            if (_buckets == null)
                Initialize(0);

            var index = FindEntry(key, hashCode, out var bucketIndex);
            if (index >= 0)
            {
                if (overwriteExisting)
                {
                    _entries[index].Value = value;
                    return true;
                }

                if (throwOnExisting)
                    throw new ArgumentException("An item with the same key has already been added.", nameof(key));

                return false;
            }

            ref var newValue = ref AddEntry(key, hashCode, bucketIndex);
            newValue = value;
            return true;
        }

        private ref TValue AddEntry(TKey key, int hashCode, int bucketIndex)
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
            entry.Key = key;
            entry.Value = default;
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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
                entry.Key = default;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
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

            throw new InvalidOperationException("The dictionary entry chain is corrupted.");
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
                newEntry.Key = oldEntry.Key;
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

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly Dictionary2<TKey, TValue> _dictionary;
            private readonly Entry[] _entries;
            private int _index;
            private int _currentIndex;

            internal Enumerator(Dictionary2<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _entries = dictionary._entries;
                _index = -1;
                _currentIndex = -1;
            }

            public readonly TKey Key
            {
                get
                {
                    CheckCurrent();
                    return _entries[_currentIndex].Key;
                }
            }

            public readonly ref TValue Value
            {
                get
                {
                    CheckCurrent();
                    return ref _entries[_currentIndex].Value;
                }
            }

            public readonly KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    CheckCurrent();
                    return new KeyValuePair<TKey, TValue>(_entries[_currentIndex].Key, _entries[_currentIndex].Value);
                }
            }

            object IEnumerator.Current => new DictionaryEntry(Key, Value);
            DictionaryEntry IDictionaryEnumerator.Entry => new(Key, Value);
            object IDictionaryEnumerator.Key => Key;
            object IDictionaryEnumerator.Value => Value;

            public bool MoveNext()
            {
                CheckStorage();
                while (++_index < _dictionary._count)
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
                _dictionary.RemoveEntryAt(_currentIndex);
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
                if (!ReferenceEquals(_entries, _dictionary._entries))
                    throw new InvalidOperationException("Collection was resized during enumeration.");
            }

            private readonly void CheckCurrent()
            {
                CheckStorage();
                if ((uint)_currentIndex >= (uint)_dictionary._count || _entries[_currentIndex].Next < -1)
                    throw new InvalidOperationException("Enumeration has not started or has already finished.");
            }

            public readonly override string ToString()
            {
                return Current.ToString();
            }
        }

        #region IDictionary and collection interfaces

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;
        ICollection IDictionary.Keys => (ICollection)Keys;
        ICollection IDictionary.Values => (ICollection)Values;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => GetValueRef(key);
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(in key);
        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => TryGetValue(in key, out value);
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => GetValueRef(key);
            set => GetValueOrAdd(key) = value;
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (key is TKey typedKey && TryGetValue(in typedKey, out var value))
                    return value;
                return null;
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                if (!(key is TKey typedKey))
                    throw new ArgumentException("The key type is invalid.", nameof(key));
                if (value == null && default(TValue) == null)
                    GetValueOrAdd(typedKey) = default;
                else
                    GetValueOrAdd(typedKey) = (TValue)value;
            }
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => Add(key, in value);
        bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(in key);
        bool IDictionary<TKey, TValue>.Remove(TKey key) => Remove(in key);
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => TryGetValue(in key, out value);
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item))
                return false;
            return Remove(item.Key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ValidateCopyTo(array, arrayIndex);
            var index = arrayIndex;
            for (var i = 0; i < _count; i++)
            {
                if (_entries[i].Next >= -1)
                    array[index++] = new KeyValuePair<TKey, TValue>(_entries[i].Key, _entries[i].Value);
            }
        }

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            var typedKey = (TKey)key;
            var typedValue = (TValue)value;
            Add(typedKey, in typedValue);
        }

        bool IDictionary.Contains(object key)
        {
            return key is TKey typedKey && ContainsKey(in typedKey);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this);

        void IDictionary.Remove(object key)
        {
            if (key is TKey typedKey)
                Remove(in typedKey);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("Only single dimensional arrays are supported.", nameof(array));
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("The array must have a zero lower bound.", nameof(array));
            if ((uint)index > (uint)array.Length || array.Length - index < Count)
                throw new ArgumentException("The destination array is too small.", nameof(array));

            if (array is KeyValuePair<TKey, TValue>[] pairs)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(pairs, index);
                return;
            }

            if (array is DictionaryEntry[] dictionaryEntries)
            {
                for (var i = 0; i < _count; i++)
                {
                    if (_entries[i].Next >= -1)
                        dictionaryEntries[index++] = new DictionaryEntry(_entries[i].Key, _entries[i].Value);
                }

                return;
            }

            if (!(array is object[] objects))
                throw new ArgumentException("The destination array type is invalid.", nameof(array));
            try
            {
                for (var i = 0; i < _count; i++)
                {
                    if (_entries[i].Next >= -1)
                        objects[index++] = new KeyValuePair<TKey, TValue>(_entries[i].Key, _entries[i].Value);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("The destination array type is invalid.", nameof(array));
            }
        }

        private void ValidateCopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if ((uint)arrayIndex > (uint)array.Length || array.Length - arrayIndex < Count)
                throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        #endregion
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class Dictionary2DebugView<TKey, TValue>
    {
        private readonly Dictionary2<TKey, TValue> _dictionary;

        public Dictionary2DebugView(Dictionary2<TKey, TValue> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] public KeyValuePair<TKey, TValue>[] Items => _dictionary.ToArray();
    }
}