// // ==================== qcbf@qq.com | 2026-07-02 ====================

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FLib
{
    public class TypescriptConfigBuilder : IConfigBuildable
    {
        public string Extension => ".ts";

        public void Build(ConfigBuilderTable table, ConfigBuilderFile file)
        {
            var json = ConfigTypescriptHelper.Compile(file.Path);
            foreach (var item in Json5.Deserialize<Json5ConfigBuilder.Value[]>(json, new Json5DeserializeOptionData { UserData = table }))
                table.AddConfig(item.KeyValue, item.CfgData);
        }
    }
}