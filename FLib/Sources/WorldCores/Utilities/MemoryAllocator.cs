// ==================== qcbf@qq.com | 2026-01-04 ====================

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FLib.WorldCores
{
    public sealed unsafe class MemoryAllocator
    {
        public readonly uint ChunkSize;
        public readonly uint ChunksPerPage;
        public readonly uint Alignment;

        private readonly Page _pages;
        private readonly ConcurrentStack<IntPtr> _freePages = new();

        public int FreePagesCount => _freePages.Count;

        private class Page
        {
            public readonly byte* FirstPtr;
            public byte ChunkCount;
            public Page Next;
            public Page(MemoryAllocator allocator) => FirstPtr = (byte*)WorldGlobalSetting.MemAlloc(allocator.ChunkSize * allocator.ChunksPerPage, allocator.Alignment);
        }

        /// <summary>
        /// 
        /// </summary>
        public MemoryAllocator(uint chunkSize, uint chunksPerPage, uint alignment = 16)
        {
            ChunkSize = chunkSize;
            ChunksPerPage = chunksPerPage;
            Alignment = alignment;
            _pages = new Page(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public byte* Alloc()
        {
            if (_freePages.TryPop(out var ptr))
                return (byte*)ptr;
            var page = _pages;
            lock (_pages)
            {
                if (_freePages.TryPop(out ptr))
                    return (byte*)ptr;
                while (page != null)
                {
                    if (ChunksPerPage - page.ChunkCount > 0)
                        return page.FirstPtr + page.ChunkCount++ * ChunkSize;

                    page.Next ??= new Page(this);
                    page = page.Next;
                }
            }

            throw new Exception("not found chunk");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Free(ref byte* ptr)
        {
            Free(ptr);
            ptr = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Free(byte* ptr)
        {
            Debug.Assert(ptr != null);
            _freePages.Push((IntPtr)ptr);
        }
    }
}