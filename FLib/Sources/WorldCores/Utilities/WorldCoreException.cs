// ==================== qcbf@qq.com | 2026-03-01 ====================

using System;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    public class WorldCoreException : Exception
    {
        public WorldCore World;
        public WorldEntityId EntityId;

        public WorldCoreException(in WorldEntity entity, object msg, Exception inner = null) : this(entity.World, entity.EntityId, msg, inner)
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
    }
}