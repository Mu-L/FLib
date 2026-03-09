// ==================== qcbf@qq.com |2026-01-03 ====================

// ReSharper disable StaticMemberInGenericType

namespace FLib.WorldCores
{
    public static class WorldComponentGenericMap<T>
    {
        public static WorldComponentMeta Meta { get; internal set; }
        public static WorldComponentInfo Info;
        public static WorldIncrementId Id => Meta.Id;
        public static ushort Size => Meta.Size;
        public static bool IsEmpty => Meta.Id.IsEmpty;

        internal static WorldComponentMeta Init(WorldComponentMeta meta)
        {
            Info = WorldComponentRegistry.GetInfo(meta);
            Meta = meta;
            return Meta;
        }
    }
}