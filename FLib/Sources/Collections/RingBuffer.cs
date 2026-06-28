//=================================================={By Qcbf|qcbf@qq.com|3/29/2025 3:28:41 PM}==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib
{
    /// <summary>固定长度环形缓冲: 写满后覆写最老元素。容量由 <see cref="Resize"/> 手动扩容, <see cref="Add"/> 不会自动扩容。逻辑索引 0 = 最老, Count-1 = 最新。</summary>
    public struct RingBuffer<T> : IList<T>
    {
        private T[] _buffer;
        private int _head; // 下一个写入位置(物理)
        private int _count; // 有效元素数 [0, Capacity]

        /// <summary>总容量(物理数组长度)。</summary>
        public readonly int Capacity => _buffer?.Length ?? 0;

        /// <summary>有效元素数量。</summary>
        public readonly int Count => _count;

        readonly bool ICollection<T>.IsReadOnly => false;

        /// <summary>按逻辑索引访问(0 = 最老)。返回 ref, 可读可写。</summary>
        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref _buffer[GetPhysicalIndex(index)];
            }
        }

        T IList<T>.this[int index]
        {
            readonly get => this[index];
            set => this[index] = value;
        }

        public RingBuffer(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = capacity == 0 ? Array.Empty<T>() : new T[capacity];
            _head = 0;
            _count = 0;
        }

        /// <summary>扩容(仅增长)。现有元素按逻辑顺序紧凑拷贝到新缓冲头部, 之后逻辑索引 0 对齐物理 0。</summary>
        public void Resize(int newCapacity)
        {
            if (newCapacity <= Capacity) return;
            var newBuffer = new T[newCapacity];
            if (_count > 0)
            {
                var cap = _buffer.Length;
                var start = _head - _count;
                if (start < 0) start += cap;
                var first = cap - start;
                if (first > _count) first = _count;
                Array.Copy(_buffer, start, newBuffer, 0, first);
                var rest = _count - first;
                if (rest > 0)
                    Array.Copy(_buffer, 0, newBuffer, first, rest);
            }

            _buffer = newBuffer;
            _head = _count < newCapacity ? _count : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AddCore(in T val)
        {
            var buf = _buffer;
            if (buf == null || buf.Length == 0)
                throw new InvalidOperationException("RingBuffer not initialized, call Resize first.");
            var cap = buf.Length;
            var phys = _head;
            buf[phys] = val;
            if (++_head == cap) _head = 0;
            if (_count < cap) _count++;
            return phys;
        }

        /// <summary>写入一个元素, 满则覆写最老元素。返回新元素的逻辑索引(恒为 Count-1)。</summary>
        public int Add(in T val)
        {
            AddCore(val);
            return _count - 1;
        }

        /// <summary>分配一个槽位并返回其引用供原地填充。out index 为该槽逻辑索引。</summary>
        public ref T AddEmpty(out int index)
        {
            var phys = AddCore(default);
            index = _count - 1;
            return ref _buffer[phys];
        }

        void ICollection<T>.Add(T item) => Add(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetPhysicalIndex(int index)
        {
            var phys = _head - _count + index;
            if (phys < 0) phys += _buffer.Length;
            return phys;
        }

        /// <summary>按逻辑索引插入。满时和 Add 一样踢掉最老元素。</summary>
        public void Insert(int index, in T item)
        {
            if (index < 0 || index > _count) throw new ArgumentOutOfRangeException(nameof(index));
            var buf = _buffer;
            if (buf == null || buf.Length == 0)
                throw new InvalidOperationException("RingBuffer not initialized, call Resize first.");
            var cap = buf.Length;
            if (_count == 0)
            {
                AddCore(item);
                return;
            }

            var start = _head - _count;
            if (start < 0) start += cap;
            var oldCount = _count;
            var full = oldCount == cap;
            if (!full)
            {
                var last = start + oldCount;
                if (last >= cap) last -= cap;
                for (var i = oldCount; i > index; i--)
                {
                    var to = start + i;
                    if (to >= cap) to -= cap;
                    var from = to - 1;
                    if (from < 0) from += cap;
                    buf[to] = buf[from];
                }

                var phys = start + index;
                if (phys >= cap) phys -= cap;
                buf[phys] = item;
                _count = oldCount + 1;
                _head = last + 1;
                if (_head == cap) _head = 0;
                return;
            }

            if (index == 0)
            {
                buf[start] = item;
                return;
            }

            for (var i = 0; i < index - 1; i++)
            {
                var to = start + i;
                if (to >= cap) to -= cap;
                var from = to + 1;
                if (from == cap) from = 0;
                buf[to] = buf[from];
            }

            var insertPhys = start + index - 1;
            if (insertPhys >= cap) insertPhys -= cap;
            buf[insertPhys] = item;
        }

        void IList<T>.Insert(int index, T item) => Insert(index, item);

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
            var buf = _buffer;
            var cap = buf.Length;
            var start = _head - _count;
            if (start < 0) start += cap;
            for (var i = index; i < _count - 1; i++)
            {
                var to = start + i;
                if (to >= cap) to -= cap;
                var from = to + 1;
                if (from == cap) from = 0;
                buf[to] = buf[from];
            }

            var last = start + _count - 1;
            if (last >= cap) last -= cap;
            buf[last] = default;
            _count--;
            _head = last;
        }

        void IList<T>.RemoveAt(int index) => RemoveAt(index);

        public bool Remove(in T item)
        {
            var index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        bool ICollection<T>.Remove(T item) => Remove(item);

        public void Clear() => Clear(true);

        /// <summary>清空。isClearMemory = true 时同时把物理数组清为默认值。</summary>
        public void Clear(bool isClearMemory)
        {
            if (isClearMemory && _buffer != null)
                Array.Fill(_buffer, default);
            _head = 0;
            _count = 0;
        }

        public readonly bool Contains(in T item)
        {
            if (_count == 0) return false;
            var comparer = EqualityComparer<T>.Default;
            var cap = _buffer.Length;
            var start = _head - _count;
            if (start < 0) start += cap;
            for (var i = 0; i < _count; i++)
            {
                var phys = start + i;
                if (phys >= cap) phys -= cap;
                if (comparer.Equals(_buffer[phys], item)) return true;
            }

            return false;
        }

        readonly bool ICollection<T>.Contains(T item) => Contains(item);

        public readonly int IndexOf(in T item)
        {
            if (_count == 0) return -1;
            var comparer = EqualityComparer<T>.Default;
            var cap = _buffer.Length;
            var start = _head - _count;
            if (start < 0) start += cap;
            for (var i = 0; i < _count; i++)
            {
                var phys = start + i;
                if (phys >= cap) phys -= cap;
                if (comparer.Equals(_buffer[phys], item)) return i;
            }

            return -1;
        }

        readonly int IList<T>.IndexOf(T item) => IndexOf(item);

        public readonly void CopyTo(T[] array, int arrayIndex)
        {
            if (_count == 0) return;
            var cap = _buffer.Length;
            var start = _head - _count;
            if (start < 0) start += cap;
            var first = cap - start;
            if (first > _count) first = _count;
            Array.Copy(_buffer, start, array, arrayIndex, first);
            var rest = _count - first;
            if (rest > 0)
                Array.Copy(_buffer, 0, array, arrayIndex + first, rest);
        }

        /// <summary>零分配枚举器, 顺序为最老 → 最新。</summary>
        public struct Enumerator : IEnumerator<T>
        {
            public T[] Buffer;
            public int Count;
            public int Start;
            public int Capacity;
            private int _logical;

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var phys = Start + _logical - 1;
                    if (phys >= Capacity) phys -= Capacity;
                    return Buffer[phys];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_logical < Count)
                {
                    _logical++;
                    return true;
                }

                return false;
            }

            public void Reset() => _logical = 0;

            public void Dispose()
            {
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            var buffer = _buffer;
            var cap = buffer == null ? 0 : buffer.Length;
            var start = _head - _count;
            if (start < 0) start += cap;
            return new Enumerator
            {
                Buffer = buffer,
                Count = _count,
                Start = start,
                Capacity = cap,
            };
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
