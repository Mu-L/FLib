// ==================== qcbf@qq.com |2025-12-11 ====================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using FLib.WorldCores.SoaComponents;
using FLib.WorldCores.Queries;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Archetypes;
using FLib.WorldCores.Behaviors;

namespace FLib.WorldCores
{
    public partial class WorldCore : FEvent, IDisposable, IEnumerable<WorldEntity>
    {
        /// <summary>
        /// 所有世界核心实例的全局列表。
        /// </summary>
        public static FixedIndexList<WorldCore> AllWorlds;

        private static ushort _worldVersionIncrement;
        private static SpinLock _locker;

        /// <summary>
        /// 世界的唯一句柄标识。
        /// </summary>
        public WorldHandle Handle;

        /// <summary>
        /// 原型群组，管理世界中的所有实体原型。
        /// </summary>
        public readonly WorldArchetypeGroup ArchetypeGroup;

        /// <summary>
        /// 面向数据的组件群组管理器。
        /// </summary>
        public readonly WorldSoaComponentGroupManager Soa;

        /// <summary>
        /// 动态组件的稀疏表示列表。
        /// </summary>
        public FixedIndexList<PooledList<int>> DynamicComponentSparse;

        /// <summary>
        /// 实体容器，管理世界中的所有实体。
        /// </summary>
        public WorldEntityContainer Entities;

        /// <summary>
        /// 第一个更新器，用于处理第一阶段的逻辑更新。
        /// </summary>
        public WorldUpdater Update1;

        /// <summary>
        /// 第二个更新器，用于处理第二阶段的逻辑更新。
        /// </summary>
        public WorldUpdater Update2;

        /// <summary>
        /// 当前世界的逻辑时间（帧数 × 时间增量）。
        /// </summary>
        public FNum Time;

        /// <summary>
        /// 当前世界的帧数计数器。
        /// </summary>
        public uint Frame;

        /// <summary>
        /// 
        /// </summary>
        internal ushort VersionIncrement;

        /// <summary>
        /// 生成并返回新的版本号。
        /// </summary>
        /// <returns>新生成的版本号</returns>
        internal ushort GenVersion() => unchecked(++VersionIncrement == 0 ? ++VersionIncrement : VersionIncrement);

        public bool IsDisposed => Handle.IsEmpty;

        /// <summary>
        /// 初始化一个新的世界核心实例。
        /// </summary>
        /// <param name="entityCapacity">实体容器的初始容量（默认值为 1024）</param>
        public WorldCore(int entityCapacity = 1024)
        {
            Update2 = new WorldUpdater();
            Update1 = new WorldUpdater();
            ArchetypeGroup = new WorldArchetypeGroup(this);
            Soa = new WorldSoaComponentGroupManager(this);
            Entities = new WorldEntityContainer(this, entityCapacity);
            DynamicComponentSparse = new(entityCapacity >> 1);

            var isLocking = false;
            _locker.Enter(ref isLocking);
            try
            {
                while (++_worldVersionIncrement == 0)
                {
                }

                Handle = new WorldHandle(checked((ushort)AllWorlds.Add(this)), _worldVersionIncrement);
            }
            finally
            {
                if (isLocking)
                    _locker.Exit(false);
            }
        }

        /// <summary>
        /// 获取世界中所有实体的枚举器（显式接口实现）。
        /// </summary>
        /// <returns>实体的枚举器</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 获取世界中所有实体的枚举器。
        /// </summary>
        /// <returns>实体的枚举器</returns>
        public IEnumerator<WorldEntity> GetEnumerator()
        {
            var count = Entities.Count;
            for (ushort i = 0; count > 0; i++)
            {
                if (Entities[i].IsEmpty) continue;
                --count;
                yield return new WorldEntity(i, Entities[i].Version);
            }
        }

        /// <summary>
        /// 创建一个新的查询过滤器生成器。
        /// </summary>
        /// <returns>查询过滤器生成器实例</returns>
        public WorldQueryFilterBuilder BuildQuery() => new(this);

        /// <summary>
        /// 使用指定的过滤器执行查询，返回匹配实体的枚举器。
        /// </summary>
        /// <param name="filter">查询过滤器条件</param>
        /// <returns>查询枚举器</returns>
        public WorldQueryEnumerator Query(in WorldQueryFilter filter = default) => new(this, filter);

        /// <summary>
        /// 创建一个新的实体生成器。
        /// </summary>
        /// <returns>实体生成器实例</returns>
        public WorldEntityBuilder BuildEntity()
        {
            return new WorldEntityBuilder(this);
        }

        /// <summary>
        /// 执行一次世界更新，包含增加帧数和运行所有更新器。
        /// </summary>
        public void Update()
        {
            ++Frame;
            Time += WorldGlobalSetting.DeltaTime;
            Update1.Update(this);
            Update2.Update(this);
        }

        /// <summary>
        /// 释放世界的所有资源，包括清除所有实体和原型。
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            foreach (var et in this)
            {
                try
                {
                    RemoveEntity(et);
                }
                catch (Exception e)
                {
                    Log.Error?.Write($"{et}  {e}", nameof(WorldCore), nameof(Dispose));
                }
            }

            for (ushort i = 0; i < ArchetypeGroup.Count; i++)
            {
                try
                {
                    ArchetypeGroup[i].Dispose();
                }
                catch (Exception e)
                {
                    Log.Error?.Write($"{i}  {e}", nameof(WorldCore), nameof(Dispose));
                }
            }

            for (var i = 0; i < DynamicComponentSparse.Count; i++)
                DynamicComponentSparse[i].Dispose();

            var isLocking = false;
            _locker.Enter(ref isLocking);
            try
            {
                AllWorlds.RemoveAt(Handle.Index);
                Handle = default;
            }
            finally
            {
                if (isLocking)
                    _locker.Exit(false);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ThrowException(object msg, WorldEntity entity = default, Exception inner = null)
        {
            throw new WorldCoreException(this, entity, msg, inner);
        }

        /// <summary>
        /// 
        /// </summary>
        [Conditional("DEBUG")]
        public virtual void Assert(bool conditional, WorldEntity entity = default, object msg = null, Exception inner = null)
        {
            if (!conditional) ThrowException("[world assert failed]" + msg, entity, inner);
        }

        /// <summary>
        /// 
        /// </summary>
        public static implicit operator WorldHandle(WorldCore world) => world.Handle;
    }
}