// ==================== qcbf@qq.com | 2026-04-07 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

    public record ConfigGenerateParams(string SourceDirPath, string DestDirPath, string Namespace, EConfigGenerateOption Options = EConfigGenerateOption.Default, string[] Usings = null)
    {
        public bool HasNamespace => !string.IsNullOrEmpty(Namespace);
        public bool Op(EConfigGenerateOption op) => (Options & op) != EConfigGenerateOption.None;
    }

    public static class ConfigGenerator
    {
        public static Action OnGenerateProcess;
        public static Func<string, Json5AnyValue, StringBuilder, bool> OnGenerateSchemaHook;

        /// <summary>
        /// 
        /// </summary>
        public static void Generate(ConfigGenerateParams p)
        {
            var sw = Stopwatch.StartNew();
            if (p.Op(EConfigGenerateOption.Clear))
            {
                foreach (var item in Directory.GetFiles(p.DestDirPath, "*.CfgGen.cs", SearchOption.TopDirectoryOnly))
                    File.Delete(item);
            }

            ProcessDefines(p);
            var configFiles = Directory.GetFiles(p.SourceDirPath, "*.schema.ts", SearchOption.AllDirectories);
            configFiles.AsParallel().ForAll(jsonPath => ProcessConfig(jsonPath, p));
            // foreach (var jsonPath in configFiles)
                // ProcessConfig(jsonPath, p);
            Log.Info?.Write($"generate config[{configFiles.Length + 1}] {sw.ElapsedMilliseconds}ms {p}", nameof(ConfigGenerator));
            OnGenerateProcess?.Invoke();
        }

        /// <summary>  </summary>
        public static void ProcessConfig(string jsonPath, ConfigGenerateParams p)
        {
            try
            {
                var json = ReadJson(jsonPath, p, out var strbuf);
                var args = CommandLineHelper.ToDictionary(json.TryGet("Args")?.Array.Select(v => (string)v));
                strbuf.WriteConfigClassHead(Path.GetFileNameWithoutExtension(jsonPath)[..^7], json, out var indent, out var name);
                strbuf.AppendLine().Indent(indent).AppendLine("{");
                var isStaticFields = args.ContainsKey("static");
                var indent1 = ++indent;
                foreach (var (key, fieldValue) in json["Members"].Dict)
                {
                    CommandLineHelper.ToDictionary(fieldValue.TryGet("Args")?.Array.Select(v => (string)v), args);
                    strbuf.AppendComment(indent1, fieldValue);
                    strbuf.Indent(indent1).Append("[BytesPackGenField] ").Append("public ");
                    if (isStaticFields)
                        strbuf.Append("static ");
                    strbuf.Append(fieldValue["Type"].ToString()).Append(' ').Append(key);
                    if (p.Op(EConfigGenerateOption.UseProperty))
                        strbuf.Append(" { get; private set; }").AppendDefaultValue(args);
                    else
                        strbuf.AppendDefaultValue(args)?.Append(';');

                    strbuf.AppendLine().AppendLine();
                }

                strbuf.WriteConfigToString(indent, json);
                strbuf.Indent(--indent).AppendLine("}");
                strbuf.WriteGeneratedFile(p, $"{name}.CfgGen.cs");
            }
            catch (Exception e)
            {
                throw new Exception(jsonPath, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ProcessDefines(ConfigGenerateParams p)
        {
            var json = ReadJson("./src/Declares.ts", p, out var strbuf);
            var indent = 1;
            var args = new Dictionary<string, string>();
            foreach (var item in json["Members"].Dict)
            {
                CommandLineHelper.ToDictionary(item.Value.TryGet("Args")?.Array.Select(v => (string)v), args);
                if (args.ContainsKey("ignore"))
                    continue;
                Json5AnyValue fields;
                switch ((string)item.Value["Type"])
                {
                    case "interface":
                        strbuf.AppendComment(indent, item.Value);
                        strbuf.Indent(indent).AppendLine("[BytesPackGen]");
                        strbuf.Indent(indent).Append("public partial ").Append(args.ContainsKey("class") ? "class" : "struct").Append(' ').Append(item.Key).Append(" {").AppendLine();
                        ++indent;
                        if (item.Value.TryGet("Fields", out fields))
                        {
                            foreach (var field in fields.Dict)
                            {
                                strbuf.AppendComment(indent, field.Value);
                                strbuf.Indent(indent).Append("[BytesPackGenField] ")
                                    .Append("public ").Append(field.Value["Type"].ToString()).Append(' ').Append(field.Key).Append(';').AppendLine();
                            }
                        }

                        --indent;
                        strbuf.Indent(indent).Append('}').AppendLine().AppendLine();

                        break;
                    case "enum":
                        strbuf.AppendComment(indent, item.Value);
                        if (args.ContainsKey("flags"))
                            strbuf.Indent(indent).AppendLine("[Flags]");
                        strbuf.Indent(indent).Append($"public enum {item.Key}");
                        if (args.TryGetValue("base", out var baseType))
                            strbuf.Append(" : ").Append(baseType);
                        strbuf.AppendLine().Indent(indent).AppendLine("{");
                        ++indent;
                        if (item.Value.TryGet("Fields", out fields))
                        {
                            foreach (var field in fields.Dict)
                            {
                                strbuf.AppendComment(indent, field.Value);
                                strbuf.Indent(indent).Append(field.Key);
                                if (field.Value.TryGet("Value", out var value))
                                    strbuf.Append(" = ").Append(value.ToString());
                                strbuf.AppendLine(",").AppendLine();
                            }
                        }

                        --indent;
                        strbuf.Indent(indent).AppendLine("}").AppendLine();
                        break;
                }
            }

            strbuf.WriteGeneratedFile(p, "_ConfigDefines.CfgGen.cs");
        }

        /// <summary>  </summary>
        private static Json5AnyValue ReadJson(string fileName, ConfigGenerateParams p, out StringBuilder strbuf, string[] extraUsings = null)
        {
            using var proc = Process.Start(new ProcessStartInfo("node")
            {
                WorkingDirectory = Path.GetFullPath(p.SourceDirPath),
                ArgumentList = { "./tools/Compile.ts", fileName },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            var err = proc.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(err))
                throw new Exception($"{proc.StartInfo.Arguments}\n{proc.StartInfo.WorkingDirectory}\n{err}");
            var jsonText = proc.StandardOutput.ReadToEnd();
            strbuf = new StringBuilder(jsonText.Length).AppendFileHead(p, extraUsings);
            return Json5.Deserialize<Json5AnyValue>(jsonText);
        }

        /// <summary>  </summary>
        private static void WriteConfigClassHead(this StringBuilder strbuf, string fileName, Json5AnyValue json, out int indent, out string name)
        {
            indent = 1;
            name = null;
            if (json == null || json.Count == 0)
                return;
            name = json["Name"].ToString();

            strbuf.AppendComment(indent, json)
                .Indent(indent).AppendLine($"[BytesPackGen, Config(\"{fileName}\")]")
                .Indent(indent).Append("public partial class ").Append(name);
        }

        /// <summary>  </summary>
        private static void WriteConfigToString(this StringBuilder strbuf, int indent, Json5AnyValue json)
        {
            strbuf.Indent(indent).Append("public override string ToString() => ");
            var logFields = json["Members"].Dict.Keys.Take(4).Select(k => $"Json5.SerializeToLog({k})").ToArray();
            if (logFields.Length > 0)
                strbuf.Append("string.Join(\",\", new[] { ").Append(string.Join(", ", logFields)).AppendLine(" });").AppendLine();
            else
                strbuf.AppendLine("string.Empty;").AppendLine();
        }

        /// <summary>  </summary>
        private static void WriteGeneratedFile(this StringBuilder strbuf, ConfigGenerateParams p, string fileName)
        {
            if (p.HasNamespace)
                strbuf.Append('}');
            File.WriteAllText(Path.Combine(p.DestDirPath, fileName), strbuf.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        private static StringBuilder AppendDefaultValue(this StringBuilder strbuf, IDictionary<string, string> args)
        {
            if (!args.TryGetValue("defaultValue", out var value))
                return strbuf;
            strbuf.Append(" = ").Append(value).Append(';');
            return null;
        }

        /// <summary>  </summary>
        private static StringBuilder AppendComment(this StringBuilder strbuf, int indent, Json5AnyValue value, string key = "Comment")
        {
            strbuf.Indent(indent).Append("/// <summary> ")
                .Append(value.TryGet(key)?.ToString())
                .AppendLine(" </summary>");
            return strbuf;
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
        private static StringBuilder AppendFileHead(this StringBuilder strbuf, ConfigGenerateParams p, string[] extraUsings = null)
        {
            strbuf.AppendLine("// generate sources by FLib.ConfigBuilder").AppendLine()
                .AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using FLib;");
            if (extraUsings != null && extraUsings.Length > 0)
            {
                foreach (var u in extraUsings)
                    strbuf.Append("using ").Append(u).Append(';').AppendLine();
            }

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