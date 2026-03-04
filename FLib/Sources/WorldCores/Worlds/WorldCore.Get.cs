// ==================== qcbf@qq.com | 2026-03-04 ====================

using System;
using System.Diagnostics;

namespace FLib.WorldCores
{
    public unsafe partial class WorldCore
    {
        /// <summary>
        /// 
        /// </summary>
        public Components<T1, T2> GetSta<T1, T2>(in Entity et) where T1 : unmanaged where T2 : unmanaged
        {
            ref readonly var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, et, "version error");
            var chunk = eti.Chunk;
            var idx = eti.IndexInChunk;
            return new Components<T1, T2>(new Ref<T1>(chunk.Get<T1>(idx)), new Ref<T2>(chunk.Get<T2>(idx)));
        }

        /// <summary>
        /// 
        /// </summary>
        public Components<T1, T2, T3> GetSta<T1, T2, T3>(in Entity et) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            ref readonly var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, et, "version error");
            var chunk = eti.Chunk;
            var idx = eti.IndexInChunk;
            return new Components<T1, T2, T3>(new Ref<T1>(chunk.Get<T1>(idx)), new Ref<T2>(chunk.Get<T2>(idx)), new Ref<T3>(chunk.Get<T3>(idx)));
        }

        /// <summary>
        /// 
        /// </summary>
        public Components<T1, T2, T3, T4> GetSta<T1, T2, T3, T4>(in Entity et) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            ref readonly var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, et, "version error");
            var chunk = eti.Chunk;
            var idx = eti.IndexInChunk;
            return new Components<T1, T2, T3, T4>(new Ref<T1>(chunk.Get<T1>(idx)), new Ref<T2>(chunk.Get<T2>(idx)), new Ref<T3>(chunk.Get<T3>(idx)), new Ref<T4>(chunk.Get<T4>(idx)));
        }

        /// <summary>
        /// 
        /// </summary>
        public Components<T1, T2, T3, T4, T5> GetSta<T1, T2, T3, T4, T5>(in Entity et) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            ref readonly var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, et, "version error");
            var chunk = eti.Chunk;
            var idx = eti.IndexInChunk;
            return new Components<T1, T2, T3, T4, T5>(new Ref<T1>(chunk.Get<T1>(idx)), new Ref<T2>(chunk.Get<T2>(idx)), new Ref<T3>(chunk.Get<T3>(idx)), new Ref<T4>(chunk.Get<T4>(idx)), new Ref<T5>(chunk.Get<T5>(idx)));
        }

        /// <summary>
        /// 
        /// </summary>
        public Components<T1, T2, T3, T4, T5, T6> GetSta<T1, T2, T3, T4, T5, T6>(in Entity et) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            ref readonly var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, et, "version error");
            var chunk = eti.Chunk;
            var idx = eti.IndexInChunk;
            return new Components<T1, T2, T3, T4, T5, T6>(new Ref<T1>(chunk.Get<T1>(idx)), new Ref<T2>(chunk.Get<T2>(idx)), new Ref<T3>(chunk.Get<T3>(idx)), new Ref<T4>(chunk.Get<T4>(idx)), new Ref<T5>(chunk.Get<T5>(idx)), new Ref<T6>(chunk.Get<T6>(idx)));
        }

        /// <summary>
        /// 
        /// </summary>
        public Components<T1, T2, T3, T4, T5, T6, T7> GetSta<T1, T2, T3, T4, T5, T6, T7>(in Entity et) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            ref readonly var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, et, "version error");
            var chunk = eti.Chunk;
            var idx = eti.IndexInChunk;
            return new Components<T1, T2, T3, T4, T5, T6, T7>(new Ref<T1>(chunk.Get<T1>(idx)), new Ref<T2>(chunk.Get<T2>(idx)), new Ref<T3>(chunk.Get<T3>(idx)), new Ref<T4>(chunk.Get<T4>(idx)), new Ref<T5>(chunk.Get<T5>(idx)), new Ref<T6>(chunk.Get<T6>(idx)), new Ref<T7>(chunk.Get<T7>(idx)));
        }

        /// <summary>
        /// 
        /// </summary>
        public Components<T1, T2, T3, T4, T5, T6, T7, T8> GetSta<T1, T2, T3, T4, T5, T6, T7, T8>(in Entity et) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            ref readonly var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, et, "version error");
            var chunk = eti.Chunk;
            var idx = eti.IndexInChunk;
            return new Components<T1, T2, T3, T4, T5, T6, T7, T8>(new Ref<T1>(chunk.Get<T1>(idx)), new Ref<T2>(chunk.Get<T2>(idx)), new Ref<T3>(chunk.Get<T3>(idx)), new Ref<T4>(chunk.Get<T4>(idx)), new Ref<T5>(chunk.Get<T5>(idx)), new Ref<T6>(chunk.Get<T6>(idx)), new Ref<T7>(chunk.Get<T7>(idx)), new Ref<T8>(chunk.Get<T8>(idx)));
        }

    }
}