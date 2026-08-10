//=================================================={By Qcbf|qcbf@qq.com|3/29/2025 3:28:41 PM}==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib
{
    /// <summary>固定长度环形缓冲。索引就是物理槽位, 0 不保证是最老元素。</summary>
    public struct RingBuffer<T> : IEnumerable<T>
    {
        private T[] _buffer;

        /// <summary>物理数组长度。</summary>
        public readonly int Capacity => _buffer?.Length ?? 0;

        /// <summary>当前有效元素数量。</summary>
        public int Count { get; private set; }

        /// <summary>最后一次写入的物理槽位。</summary>
        public int LastWriteIndex { get; private set; }

        /// <summary>按物理槽位访问。返回 ref, 可读可写。</summary>
        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _buffer[index];
        }

        public RingBuffer(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = capacity == 0 ? Array.Empty<T>() : new T[capacity];
            LastWriteIndex = -1;
            Count = 0;
        }

        /// <summary>扩容(仅增长)。有效元素会按从旧到新的顺序重排。</summary>
        public void Resize(int newCapacity)
        {
            var oldCapacity = Capacity;
            if (newCapacity <= oldCapacity) return;

            var newBuffer = new T[newCapacity];
            if (Count > 0)
            {
                var index = GetFirstIndex(oldCapacity);
                for (var i = 0; i < Count; i++)
                {
                    newBuffer[i] = _buffer[index];
                    if (++index == oldCapacity) index = 0;
                }

                LastWriteIndex = Count - 1;
            }

            _buffer = newBuffer;
        }

        /// <summary>写入一个元素, 满则回绕覆写。返回写入的物理槽位。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(in T val)
        {
            var buf = _buffer;
            if (buf == null || buf.Length == 0)
                throw new InvalidOperationException("RingBuffer not initialized, call Resize first.");

            var index = LastWriteIndex + 1;
            if (index == buf.Length) index = 0;
            LastWriteIndex = index;
            buf[index] = val;
            if (Count < buf.Length) Count++;
            return index;
        }

        /// <summary>分配一个槽位并返回其引用供原地填充。out index 为物理槽位。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddEmpty(out int index)
        {
            var buf = _buffer;
            if (buf == null || buf.Length == 0)
                throw new InvalidOperationException("RingBuffer not initialized, call Resize first.");

            index = LastWriteIndex + 1;
            if (index == buf.Length) index = 0;
            LastWriteIndex = index;
            if (Count < buf.Length) Count++;
            return ref buf[index];
        }

        /// <summary>弹出 LastWriteIndex 槽位。空缓冲返回 default。</summary>
        public T Pop()
        {
            if (Count == 0) return default;

            var result = _buffer[LastWriteIndex];
            _buffer[LastWriteIndex] = default;
            if (--Count == 0)
                LastWriteIndex = -1;
            else
                LastWriteIndex = LastWriteIndex == 0 ? Capacity - 1 : LastWriteIndex - 1;
            return result;
        }

        /// <summary>移除指定物理槽位，并保持有效元素连续。</summary>
        public void RemoveAt(int index)
        {
            var capacity = Capacity;
            var firstIndex = GetFirstIndex(capacity);
            var offset = index - firstIndex;
            if (offset < 0) offset += capacity;
            if ((uint)offset >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));

            for (var i = offset; i < Count - 1; i++)
            {
                var to = firstIndex + i;
                if (to >= capacity) to -= capacity;
                var from = to + 1;
                if (from == capacity) from = 0;
                _buffer[to] = _buffer[from];
            }

            _buffer[LastWriteIndex] = default;
            if (--Count == 0)
                LastWriteIndex = -1;
            else
                LastWriteIndex = LastWriteIndex == 0 ? capacity - 1 : LastWriteIndex - 1;
        }

        public bool Remove(in T item)
        {
            var index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void Clear() => Clear(true);

        /// <summary>清空。isClearMemory = true 时同时把物理数组清为默认值。</summary>
        public void Clear(bool isClearMemory)
        {
            if (isClearMemory && _buffer != null)
                Array.Fill(_buffer, default);
            LastWriteIndex = -1;
            Count = 0;
        }

        public readonly bool Contains(in T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>查找并返回物理槽位。不存在返回 -1。</summary>
        public readonly int IndexOf(in T item)
        {
            if (Count == 0) return -1;

            var comparer = EqualityComparer<T>.Default;
            var index = GetFirstIndex(Capacity);
            for (var i = 0; i < Count; i++)
            {
                if (comparer.Equals(_buffer[index], item))
                    return index;
                if (++index == Capacity) index = 0;
            }

            return -1;
        }

        /// <summary>从最旧元素开始拷贝有效段。</summary>
        public readonly void CopyTo(T[] array, int arrayIndex)
        {
            var capacity = Capacity;
            var index = GetFirstIndex(capacity);
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = _buffer[index];
                if (++index == capacity) index = 0;
            }
        }

        /// <summary>零分配枚举器，从最旧元素枚举到最新元素。</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _buffer;
            private readonly int _capacity;
            private readonly int _count;
            private readonly int _firstIndex;
            private int _remaining;
            private int _index;

            public Enumerator(T[] buffer, int lastWriteIndex, int count)
            {
                _buffer = buffer;
                _capacity = buffer?.Length ?? 0;
                _count = count;
                _remaining = count;
                _firstIndex = count > 0 ? lastWriteIndex - count + 1 : -1;
                if (_firstIndex < 0) _firstIndex += _capacity;
                _index = _firstIndex == 0 ? _capacity - 1 : _firstIndex - 1;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _buffer[_index];
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_remaining-- <= 0) return false;
                if (++_index == _capacity) _index = 0;
                return true;
            }

            public void Reset()
            {
                _index = _firstIndex == 0 ? _capacity - 1 : _firstIndex - 1;
                _remaining = _count;
            }

            public void Dispose()
            {
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(_buffer, LastWriteIndex, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetFirstIndex(int capacity)
        {
            if (Count == 0) return -1;
            var firstIndex = LastWriteIndex - Count + 1;
            return firstIndex < 0 ? firstIndex + capacity : firstIndex;
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
