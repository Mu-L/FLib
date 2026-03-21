// ==================== qcbf@qq.com | 2026-03-01 ====================

using System;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    public class WorldCoreException : Exception
    {
        public WorldCore World;
        public WorldEntity Entity;

        public WorldCoreException(in WorldEntityHelper entity, object msg, Exception inner = null) : this(entity.World, entity.Entity, msg, inner)
        {
        }

        public WorldCoreException(WorldCore world, object msg, Exception inner = null) : base(msg.ToString(), inner)
        {
            World = world;
        }

        public WorldCoreException(WorldCore world, WorldEntity entity, object msg, Exception inner = null) : base(msg.ToString(), inner)
        {
            World = world;
            Entity = entity;
        }

        public override string Message => Entity.IsEmpty ? $"[{World.Frame}]{base.Message}" : $"[{World.Frame}][{Entity.ToString()}]{base.Message}";
    }
}