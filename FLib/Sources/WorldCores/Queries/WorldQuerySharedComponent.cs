// ==================== qcbf@qq.com | 2026-01-17 ====================

namespace FLib.WorldCores
{
    public readonly struct WorldQuerySharedComponent
    {
        public readonly WorldIncrementId ComponentId;
        public readonly int Hash;

        public WorldQuerySharedComponent(WorldIncrementId componentId, int hash)
        {
            ComponentId = componentId;
            Hash = hash;
        }

        // 这里留一个优化点, 如果存在大量GetSharedComponent情况再来做.
        // 主要是优化改为unity entities那种实现方式, chunk存aos index, 防止额外的hash查找开销.
        // 如果不做这个优化点那么这里比较好做直接对比hash而不用确保index的版本问题, GetSharedComponent性能稍差一点
        // public int Index;
        // public int Version;
    }
}