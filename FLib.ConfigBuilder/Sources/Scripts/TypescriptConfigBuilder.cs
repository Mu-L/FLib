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
            var json = ReadJsonText(FIO.PathTrimRightDirectory(ctx.SourceFilePath, 2), ctx.SourceFilePath);
            foreach (var item in Json5.Deserialize<Json5ConfigBuilder.Value[]>(json, new Json5DeserializeOptionData { UserData = ctx }))
                ctx.AddConfig(item.KeyValue, item.CfgData);
        }


        public static string ReadJsonText(string workingDirectory, string fileName)
        {
            using var proc = Process.Start(new ProcessStartInfo("node")
            {
                WorkingDirectory = workingDirectory,
                ArgumentList = { "./tools/Compile.ts", fileName },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            var err = proc.StandardError.ReadToEnd();
            return !string.IsNullOrEmpty(err)
                ? throw new Exception($"{proc.StartInfo.Arguments}\n{proc.StartInfo.WorkingDirectory}\n{err}")
                : proc.StandardOutput.ReadToEnd();
        }
    }
}