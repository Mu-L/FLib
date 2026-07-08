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

        /// <summary>当前按 [0..LastWriteIndex] 暴露的数量。</summary>
        public readonly int Count => LastWriteIndex + 1;

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
        }

        /// <summary>扩容(仅增长)。物理槽位保持不变。</summary>
        public void Resize(int newCapacity)
        {
            var oldCapacity = Capacity;
            if (newCapacity <= oldCapacity) return;

            var newBuffer = new T[newCapacity];
            if (oldCapacity > 0)
                Array.Copy(_buffer, 0, newBuffer, 0, oldCapacity);
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
            return ref buf[index];
        }

        /// <summary>弹出 LastWriteIndex 槽位。空缓冲返回 default。</summary>
        public T Pop()
        {
            if (LastWriteIndex < 0) return default;

            var result = _buffer[LastWriteIndex];
            _buffer[LastWriteIndex] = default;
            LastWriteIndex--;
            return result;
        }

        /// <summary>清空指定物理槽位。只有移除 LastWriteIndex 时才收缩 Count。</summary>
        public void RemoveAt(int index)
        {
            _buffer[index] = default;
            if (index == LastWriteIndex)
                LastWriteIndex--;
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
        }

        public readonly bool Contains(in T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>在 [0..LastWriteIndex] 中查找并返回物理槽位。不存在返回 -1。</summary>
        public readonly int IndexOf(in T item)
        {
            if (LastWriteIndex < 0) return -1;

            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i <= LastWriteIndex; i++)
            {
                if (comparer.Equals(_buffer[i], item))
                    return i;
            }

            return -1;
        }

        /// <summary>从物理槽位 0 开始拷贝 [0..LastWriteIndex]。</summary>
        public readonly void CopyTo(T[] array, int arrayIndex)
        {
            if (LastWriteIndex < 0) return;
            Array.Copy(_buffer, 0, array, arrayIndex, LastWriteIndex + 1);
        }

        /// <summary>零分配枚举器, 从物理槽位 0 开始枚举到 LastWriteIndex。</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _buffer;
            private readonly int _lastWriteIndex;
            private int _index;

            public Enumerator(T[] buffer, int lastWriteIndex)
            {
                _buffer = buffer;
                _lastWriteIndex = lastWriteIndex;
                _index = -1;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _buffer[_index];
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (++_index > _lastWriteIndex)
                    return false;
                return true;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose()
            {
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(_buffer, LastWriteIndex);
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
