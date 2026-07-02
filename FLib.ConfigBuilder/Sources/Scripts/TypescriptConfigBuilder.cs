// // ==================== qcbf@qq.com | 2026-07-02 ====================

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FLib
{
    public class TypescriptConfigBuilder : ConfigBuilder.IBuildable
    {
        public string Extension => ".ts";

        public void Build(in ConfigBuilder.TableContext ctx)
        {
            var json = ConfigTypescriptHelper.Compile(ctx.SourceFilePath);
            Log.Info?.Write(json, nameof(TypescriptConfigBuilder), nameof(Build));
            foreach (var item in Json5.Deserialize<Json5ConfigBuilder.Value[]>(json, new Json5DeserializeOptionData { UserData = ctx }))
                ctx.AddConfig(item.KeyValue, item.CfgData);
        }
    }
}