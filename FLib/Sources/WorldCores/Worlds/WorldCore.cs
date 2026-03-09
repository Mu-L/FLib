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
        /// 
        /// </summary>
        public static FixedIndexList<WorldCore> AllWorlds;

        private static ushort _worldVersionIncrement;
        private static SpinLock _locker;

        /// <summary>
        /// 
        /// </summary>
        public WorldHandle Handle;

        /// <summary>
        /// 
        /// </summary>
        public readonly WorldArchetypeGroup ArchetypeGroup;

        /// <summary>
        /// 
        /// </summary>
        public readonly WorldSoaComponentGroupManager Soa;

        /// <summary>
        /// 
        /// </summary>
        public FixedIndexList<PooledList<int>> DynamicComponentSparse;

        /// <summary>
        /// 
        /// </summary>
        public WorldEntityContainer Entities;

        /// <summary>
        /// 
        /// </summary>
        public WorldUpdater Update1;

        /// <summary>
        /// 
        /// </summary>
        public WorldUpdater Update2;

        /// <summary>
        /// 
        /// </summary>
        public uint Frame;

        /// <summary>
        /// 
        /// </summary>
        internal ushort VersionIncrement;

        /// <summary>
        /// 
        /// </summary>
        internal ushort GenVersion() => unchecked(++VersionIncrement == 0 ? ++VersionIncrement : VersionIncrement);

        public bool IsDisposed => Handle.IsEmpty;

        /// <summary>
        /// 
        /// </summary>
        public WorldCore(int entityCapacity = 1024)
        {
            Update2 = new WorldUpdater();
            Update1 = new WorldUpdater();
            ArchetypeGroup = new WorldArchetypeGroup(this);
            Soa = new WorldSoaComponentGroupManager(this);
            Entities = new WorldEntityContainer(entityCapacity);
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
        /// 
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        public WorldQueryFilterBuilder BuildQuery() => new(this);

        /// <summary>
        /// 
        /// </summary>
        public WorldQueryEnumerator Query(in WorldQueryFilter filter = default) => new(this, filter);

        /// <summary>
        /// 
        /// </summary>
        public WorldEntityBuilder BuildEntity()
        {
            return new WorldEntityBuilder(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Update()
        {
            ++Frame;
            Update1.Update(this);
            Update2.Update(this);
        }

        /// <summary>
        /// 
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