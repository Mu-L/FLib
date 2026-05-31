// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;
using FLib.WorldCores.Archetypes;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        // /// <summary>
        // /// 获取实体信息或空实体信息。
        // /// </summary>
        // /// <param name="et">目标实体</param>
        // /// <returns>实体信息的引用，如果实体无效则返回空信息</returns>
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public ref readonly WorldEntityInfo GetEntityInfoOrEmpty(in WorldEntityId et)
        // {
        //     ref var eti = ref Entities[et.Id];
        //     if (eti.Version != et.Version)
        //         return ref WorldEntityInfo.Empty;
        //     return ref eti;
        // }

        /// <summary>
        /// 获取实体信息。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <returns>实体信息的引用</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref WorldEntityInfo GetEntityInfo(in WorldEntityId et)
        {
            ref var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, msg: "version error");
            return ref eti;
        }

        /// <summary>
        /// 创建一个新的实体生成器。
        /// </summary>
        /// <returns>实体生成器实例</returns>
        public WorldEntityBuilder CreateEntityBuilder()
        {
            return new WorldEntityBuilder(this);
        }

        /// <summary>
        /// 从世界中移除指定的实体。
        /// </summary>
        /// <param name="et">要移除的实体</param>
        public void RemoveEntity(WorldEntityId et)
        {
            ref var eti = ref GetEntityInfo(et);
            eti.SetDestroying();

            try
            {
                WorldGlobalSetting.OnRemoveEntityEvent?.Invoke(et.AsEntity(this));
            }
            catch (Exception e)
            {
                Log.Error?.Write(e.ToString(), nameof(WorldCore));
            }

            if (eti.HasDynamicComponent)
            {
                var sparse = DynamicComponentSparse[eti.DynamicComponentSparseIndex];
                for (var i = 0; i < sparse.Count; i++)
                {
                    ref var denseIndex = ref sparse[i];
                    if (denseIndex < 0) continue;
                    var type = WorldComponentRegistry.GetType(new WorldIncrementId(i + 1));
                    Soa.GetGroup(type).Free(et, denseIndex, true);
                    denseIndex = -1;
                }
            }

            ArchetypeGroup[eti.ArchetypeIndex].RemoveEntity(eti);
            Entities.Remove(et.Id);
        }

        /// <summary>
        /// 检查实体是否存在于世界中。
        /// </summary>
        /// <param name="et">要检查的实体</param>
        /// <returns>如果实体存在返回 true，否则返回 false</returns>
        public bool HasEntity(WorldEntityId et)
        {
            return !et.IsEmpty && Entities.Count > et.Id && Entities[et.Id].Version == et.Version;
        }

        /// <summary>
        /// 检查实体是否存在且未处于销毁中。
        /// </summary>
        /// <param name="et">要检查的实体</param>
        /// <returns>如果实体存在且未销毁返回 true，否则返回 false</returns>
        public bool HasEntityAndNotDestroying(WorldEntityId et)
        {
            if (et.IsEmpty) return false;
            if (Entities.Count <= et.Id) return false;
            ref readonly var eti = ref Entities[et.Id];
            return eti.Version == et.Version && !eti.IsDestroying;
        }

        /// <summary>
        /// 获取实体的所有组件对象。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="list">用于存储组件的列表，为 null 时会创建新列表</param>
        /// <returns>包含实体所有组件对象的列表</returns>
        public IList<object> GetAllEntities(IList<object> list = null)
        {
            throw new NotImplementedException();
        }
    }
}