// // ==================== qcbf@qq.com | 2026-07-04 ====================

namespace FLib
{
    public interface IConfigBuildable
    {
        string Extension { get; }
        void Build(ConfigBuilderTable table, ConfigBuilderFile file);
    }
}