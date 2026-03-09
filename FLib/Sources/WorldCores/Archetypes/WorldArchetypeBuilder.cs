// ==================== qcbf@qq.com |2026-01-02 ====================

using System;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    [StructLayout(LayoutKind.Auto)]
    public struct WorldArchetypeBuilder : IDisposable
    {
        public PooledList<WorldComponentMeta> ComponentTypes;
        public ushort ComponentsSize;
        public WorldIncrementId MaxComponentId;
#if DEBUG
        private PooledHashSet<ushort> _componentIds;
#endif

        public WorldArchetypeBuilder(int componentCapacity = 8)
        {
            ComponentTypes = new PooledList<WorldComponentMeta>(componentCapacity);
            ComponentsSize = 0;
            MaxComponentId = default;
#if DEBUG
            _componentIds = default;
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            ComponentTypes.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public void With(in WorldComponentMeta meta)
        {
#if DEBUG
            if (!_componentIds.Add(meta.Id))
                throw new InvalidOperationException($"Component {meta.Type} already exists.");
#endif
            ComponentsSize += meta.Size;
            if (meta.Id > MaxComponentId)
                MaxComponentId = meta.Id;
            ComponentTypes.Add(meta);
        }
    }
}