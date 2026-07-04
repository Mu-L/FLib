// // ==================== qcbf@qq.com | 2026-07-04 ====================

using System.Collections.Generic;

namespace FLib
{
    /// <summary> 配置文件 </summary>
    public class ConfigBuilderFile : IConfigFile
    {
        /// <summary> 文件路径 </summary>
        public string Path { get; set; }

        /// <summary> 文件名 </summary>
        public string Name { get; set; }

        /// <summary> 文件标志 </summary>
        public char FileSign { get; set; }

        /// <summary> 有`.`分割的文件名 </summary>
        public List<string> Args { get; set; }

        /// <summary> 配置构建器 </summary>
        public IConfigBuildable Builder;

        public override string ToString() => $"{Name} {Path}";
    }
}