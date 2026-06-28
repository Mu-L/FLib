// ==================== qcbf@qq.com | 2026-03-01 ====================

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Behaviors;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    public class WorldCoreException : Exception
    {
        public WorldCore World;
        public WorldEntityId EntityId;

        public WorldCoreException(in WorldEntity entity, object msg, Exception inner = null) : this(entity.World, entity.Id, msg, inner)
        {
        }

        public WorldCoreException(WorldCore world, object msg, Exception inner = null) : base(msg.ToString(), inner)
        {
            World = world;
        }

        public WorldCoreException(WorldCore world, WorldEntityId eId, object msg, Exception inner = null) : base(msg.ToString(), inner)
        {
            World = world;
            EntityId = eId;
        }

        public override string Message => EntityId.IsEmpty ? $"[{World.Frame}]{base.Message}" : $"[{World.Frame}][{EntityId.ToString()}]{base.Message}";


        /// <summary>
        /// 
        /// </summary>
        [Conditional("DEBUG")]
        public static unsafe void AssertNotCopied<T>(in WorldEntity et, in T selfComponent) where T : unmanaged
        {
            if (Unsafe.AsPointer(ref Unsafe.AsRef(in selfComponent)) != Unsafe.AsPointer(ref et.GetStaRef<T>()))
                et.World.ThrowException($"{typeof(T)} was copied. Use ref WorldBehaviorSystem.", et);
        }
    }
}