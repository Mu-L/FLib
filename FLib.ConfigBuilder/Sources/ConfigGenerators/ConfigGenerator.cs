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
    [Flags]
    public enum EConfigGenerateOption
    {
        None,
        Clear = 1 << 0,
        UseProperty = 1 << 1,
        Default = Clear | UseProperty,
    }

    public record ConfigGenerateParams(
        string SourceDirPath,
        string DestDirPath,
        string Namespace,
        EConfigGenerateOption Options = EConfigGenerateOption.Default,
        string[] Usings = null)
    {
        public bool HasNamespace => !string.IsNullOrEmpty(Namespace);
        public bool Op(EConfigGenerateOption op) => (Options & op) != EConfigGenerateOption.None;
    }

    public static class ConfigGenerator
    {
        private static int _finishedTaskCount;
        public static int FinishedTaskCount => _finishedTaskCount;
        public static int TotalTasks { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public static async Task Process(ConfigGenerateParams p)
        {
            Log.Info?.Write($"generate config {p}", nameof(ConfigGenerator));
            TotalTasks = _finishedTaskCount = 0;
            if (p.Op(EConfigGenerateOption.Clear))
            {
                foreach (var item in Directory.GetFiles(p.DestDirPath, "*.Gen.cs", SearchOption.TopDirectoryOnly))
                {
                    Log.Info?.Write($"remove config {item}", nameof(ConfigGenerator));
                    File.Delete(item);
                }
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
            if (json == null || json.Count == 0)
                return;
            var indent = 1;
            var name = json["Name"].ToString();
            var fileName = Path.GetFileNameWithoutExtension(jsonPath)[..^7];
            Log.Info?.Write($"Generate Config {name} {fileName}", nameof(ConfigGenerator));

            strbuf.Indent(indent).AppendLine($"[BytesPackGen, Config(\"{fileName}\")]")
                .Indent(indent).Append("public partial class ").Append(name).AppendLine().Indent(indent).AppendLine("{");
            ++indent;

            foreach (var (key, fieldValue) in json["Fields"].Dict)
            {
                strbuf.AppendBlockComment(indent, fieldValue);
                strbuf.Indent(indent).Append("[BytesPackGenField] ")
                    .Append("public ").Append(fieldValue["Type"].ToString()).Append(' ')
                    .Append(key);
                if (p.Op(EConfigGenerateOption.UseProperty))
                    strbuf.Append(" { get; private set; }").AppendDefaultValue(fieldValue);
                else
                    strbuf.AppendDefaultValue(fieldValue)?.Append(';');

                strbuf.AppendLine().AppendLine();
            }

            strbuf.Indent(indent).Append("public override string ToString() => ");
            var logFields = json["Fields"].Dict.Keys.Take(4).Select(k => $"Json5.SerializeToLog({k})").ToArray();
            if (logFields.Length > 0)
                strbuf.Append("string.Join(\",\", new[] { ").Append(string.Join(", ", logFields)).AppendLine(" });").AppendLine();
            else
                strbuf.AppendLine("string.Empty;").AppendLine();

            --indent;
            strbuf.Indent(indent).AppendLine("}");
            if (p.HasNamespace)
                strbuf.Append('}');
            await File.WriteAllTextAsync(Path.Combine(p.DestDirPath, $"{name}.Gen.cs"), strbuf.ToString());
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
            foreach (var item in json["Enums"].Dict)
            {
                Log.Info?.Write($"Generate Define Enum {item.Key}", nameof(ConfigGenerator));
                strbuf.AppendBlockComment(indent, item.Value);
                var isFlags = item.Value["IsFlags"];
                if (isFlags)
                    strbuf.Indent(indent).AppendLine("[Flags]");
                strbuf.Indent(indent).AppendLine($"public enum {item.Key}").Indent(indent).AppendLine("{");
                ++indent;
                if (item.Value.TryGet("Fields", out var fields))
                {
                    var index = 0;
                    foreach (var field in fields.Dict)
                    {
                        strbuf.AppendBlockComment(indent, field.Value);
                        strbuf.Indent(indent).Append(field.Key);
                        if (field.Value.TryGet("Value", out var value))
                        {
                            strbuf.Append(" = ").Append(value.ToString());
                        }
                        else if (isFlags)
                        {
                            strbuf.Append(" = ").Append("1 << ").Append(index++);
                        }

                        strbuf.AppendLine(",").AppendLine();
                    }
                }

                --indent;
                strbuf.Indent(indent).AppendLine("}").AppendLine();
            }

            // 生成类型
            if (json["Types"].TryGet("GenCodeTypes", out var types))
            {
                foreach (var item in types.AsDict!)
                {
                    Log.Info?.Write($"Generate Define Type {item.Key}", nameof(ConfigGenerator));
                    strbuf.AppendBlockComment(indent, item.Value);
                    strbuf.Indent(indent).AppendLine("[BytesPackGen]");
                    strbuf.Indent(indent).Append("public partial ").Append(item.Value["Type"].ToString()).Append(' ').Append(item.Key).Append(" {").AppendLine();
                    ++indent;
                    if (item.Value.TryGet("Fields", out var fields))
                    {
                        foreach (var field in fields.Dict)
                        {
                            strbuf.AppendBlockComment(indent, field.Value);
                            strbuf.Indent(indent).Append("[BytesPackGenField] ")
                                .Append("public ").Append(field.Value.ToString()).Append(' ').Append(field.Key).Append(';').AppendLine();
                        }
                    }

                    --indent;
                    strbuf.Indent(indent).Append('}').AppendLine().AppendLine();
                }
            }

            if (p.HasNamespace)
                strbuf.Append('}');

            await File.WriteAllTextAsync(Path.Combine(p.DestDirPath, "_ConfigDefines.Gen.cs"), strbuf.ToString());
            Interlocked.Increment(ref _finishedTaskCount);
        }

        /// <summary>
        /// 
        /// </summary>
        private static StringBuilder AppendDefaultValue(this StringBuilder strbuf, Json5AnyValue field)
        {
            if (!field.TryGet("DefaultValue", out var value))
                return strbuf;
            strbuf.Append(" = ").Append(value.ToString()).Append(';');
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AppendBlockComment(this StringBuilder strbuf, int indent, Json5AnyValue value)
        {
            strbuf.Indent(indent).AppendLine("/// <summary>");
            strbuf.Indent(indent).Append("/// ").Append(value.TryGet("Comment")?.ToString()).AppendLine();
            strbuf.Indent(indent).AppendLine("/// </summary>");
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
            strbuf.AppendLine("// generate sources by FLib.ConfigBuilder").AppendLine()
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
