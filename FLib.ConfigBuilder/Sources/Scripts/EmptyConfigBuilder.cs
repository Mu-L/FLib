// // ==================== qcbf@qq.com | 2026-07-04 ====================

namespace FLib
{
    public class EmptyConfigBuilder : IConfigBuildable
    {
        public static readonly EmptyConfigBuilder Default = new();
        public string Extension => "";

        public void Build(ConfigBuilderTable table, ConfigBuilderFile file)
        {
        }
    }
}