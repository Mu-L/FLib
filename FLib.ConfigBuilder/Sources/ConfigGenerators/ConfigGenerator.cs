// ==================== qcbf@qq.com | 2026-04-07 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if UNITY_2022_3_OR_NEWER
namespace System.Runtime.CompilerServices
{
    static class IsExternalInit
    {
    }
}
#endif

namespace FLib
{
    public record ConfigGenerateParams(string SourceDirPath, string DestDirPath, string Namespace, bool IsClear = true, string[] Usings = null)
    {
        public bool HasNamespace => !string.IsNullOrEmpty(Namespace);
    }

    public static class ConfigGenerator
    {
        private static int _finishedTaskCount;
        public static int FinishedTaskCount => _finishedTaskCount;
        public static int TotalTasks { get; private set; }
        public static float Progress => TotalTasks == 0 ? 0 : FinishedTaskCount / (float)TotalTasks;

        /// <summary>
        /// 
        /// </summary>
        public static async Task Process(ConfigGenerateParams p)
        {
            TotalTasks = _finishedTaskCount = 0;
            if (p.IsClear)
            {
                foreach (var item in Directory.GetFiles(p.DestDirPath, "*.cs", SearchOption.TopDirectoryOnly))
                    File.Delete(item);
            }

            var tasks = new ConcurrentBag<Task>() { ProcessDefines(p) };
            Directory.GetFiles(p.SourceDirPath, "*.schema.json5", SearchOption.AllDirectories).AsParallel().ForAll(jsonPath =>
                tasks.Add(ProcessConfig(jsonPath, p)));

            TotalTasks = tasks.Count;
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task ProcessConfig(string jsonPath, ConfigGenerateParams p)
        {
            var jsonText = await File.ReadAllTextAsync(jsonPath);
            var strbuf = new StringBuilder(jsonText.Length).AppendHead(p);
            var json = Json5.Deserialize<Json5AnyValue>(jsonText);
            var indent = 1;
            var name = json["Name"].ToString();

            strbuf.Indent(indent).AppendLine($"[BytesPackGen, Config(\"{Path.GetFileNameWithoutExtension(jsonPath)[..^7]}\")]")
                .Indent(indent).Append("public partial class ").Append(name).AppendLine(" {");
            ++indent;

            foreach (var field in json["Fields"]!.Dict)
            {
                strbuf.Indent(indent).AppendLine("/// <summary>")
                    .Indent(indent).Append("/// ").Append(field.Value.TryGet("Comment")?.ToString()).AppendLine()
                    .Indent(indent).AppendLine("/// </summary>");
                strbuf.Indent(indent).Append("[BytesPackGenField] ")
                    .Append("public ").Append(field.Value["Type"].ToString()).Append(' ')
                    .Append(field.Key).Append(" { get; private set; }").AppendLine().AppendLine();
            }

            --indent;
            strbuf.Indent(indent).AppendLine("}");
            if (p.HasNamespace)
                strbuf.Append('}');
            await File.WriteAllTextAsync(Path.Combine(p.DestDirPath, $"{name}.cs"), strbuf.ToString());
            Interlocked.Increment(ref _finishedTaskCount);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task ProcessDefines(ConfigGenerateParams p)
        {
            var jsonText = await File.ReadAllTextAsync(Path.Combine(p.SourceDirPath, "_defines.json5"));
            var strbuf = new StringBuilder(jsonText.Length).AppendHead(p);
            var json = Json5.Deserialize<Json5AnyValue>(jsonText);
            var indent = 1;

            // 生成枚举
            foreach (var item in json["Enums"]!.Dict)
            {
                strbuf.Indent(indent).AppendLine("/// <summary>");
                strbuf.Indent(indent).Append("/// ").Append(item.Value.TryGet("Comment")?.ToString()).AppendLine();
                strbuf.Indent(indent).AppendLine("/// </summary>");
                var isFlags = item.Value["IsFlags"];
                if (isFlags)
                    strbuf.Indent(indent).AppendLine("[Flags]");
                strbuf.Indent(indent).AppendLine($"public enum {item.Key} {{");
                ++indent;

                var names = item.Value["Names"]!.AsArray!;
                var customValues = item.Value.TryGet("Values")?.AsDict;
                for (var i = 0; i < names.Length; i++)
                {
                    var name = names[i].ToString();
                    if (customValues != null && customValues.TryGetValue(name, out var customValue))
                        strbuf.Indent(indent).Append(name).Append(' ').Append('=').Append(' ').Append(customValue.ToString()).Append(',').AppendLine();
                    else if (isFlags)
                        strbuf.Indent(indent).Append(name).Append(' ').Append('=').Append(' ').Append("1 << ").Append(i).Append(',').AppendLine();
                    else
                        strbuf.Indent(indent).Append(name).Append(',').AppendLine();
                }

                --indent;
                strbuf.Indent(indent).AppendLine("}").AppendLine();
            }

            // 生成类型
            if (json["Types"].TryGet("GenCodeTypes", out var types))
            {
                foreach (var item in types.AsDict!)
                {
                    strbuf.Indent(indent).AppendLine("/// <summary>")
                        .Indent(indent).Append("/// ").Append(item.Value.TryGet("Comment")?.ToString()).AppendLine()
                        .Indent(indent).AppendLine("/// </summary>");
                    strbuf.Indent(indent).AppendLine("[BytesPackGen]");
                    strbuf.Indent(indent).Append("public partial ").Append(item.Value["Type"].ToString()).Append(' ').Append(item.Key).Append(" {").AppendLine();
                    ++indent;
                    if (item.Value.TryGet("Fields", out var fields))
                    {
                        foreach (var field in fields.AsDict!)
                            strbuf.Indent(indent).Append("[BytesPackGenField] ")
                                .Append("public ").Append(field.Value.ToString()).Append(' ').Append(field.Key).Append(';').AppendLine();
                    }

                    --indent;
                    strbuf.Indent(indent).Append('}').AppendLine().AppendLine();
                }
            }

            if (p.HasNamespace)
                strbuf.Append('}');

            await File.WriteAllTextAsync(Path.Combine(p.DestDirPath, "_ConfigDefines.cs"), strbuf.ToString());
            Interlocked.Increment(ref _finishedTaskCount);
        }

        /// <summary>
        /// 
        /// </summary>
        private static StringBuilder Indent(this StringBuilder strbuf, int indent)
        {
            return strbuf.Append(' ', indent * 4);
        }

        /// <summary>
        /// 
        /// </summary>
        private static StringBuilder AppendHead(this StringBuilder strbuf, ConfigGenerateParams p)
        {
            strbuf.AppendLine("// generator sources")
                .AppendLine("using System;")
                .AppendLine("using FLib;");
            if (p.Usings?.Length > 0)
            {
                foreach (var u in p.Usings)
                    strbuf.Append("using ").Append(u).Append(';').AppendLine();
            }

            strbuf.AppendLine();
            if (p.HasNamespace)
                strbuf.Append("namespace ").Append(p.Namespace).AppendLine().AppendLine("{");
            return strbuf;
        }
    }
}