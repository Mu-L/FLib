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

    public class ConfigGenerateParams
    {
        public string SourceDirPath;
        public string DestDirPath;
        public string Namespace;
        public EConfigGenerateOption Options;
        public string[] Usings;
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
            p.SourceDirPath = Path.GetFullPath(p.SourceDirPath);
            p.DestDirPath = Path.GetFullPath(p.DestDirPath);

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
                var isCommentAttribute = args.ContainsKey(nameof(CommentAttribute));
                var isStaticFields = args.ContainsKey("static");
                var cfgName = json["Name"].ToString();
                var indent = 1;

                strbuf.AppendComment(indent, json)
                    .Indent(indent).AppendLine($"[BytesPackGen, Config(\"{Path.GetFileNameWithoutExtension(jsonPath)[..^7]}\")]")
                    .Indent(indent).Append("public partial class ").Append(cfgName);

                strbuf.AppendLine().Indent(indent).AppendLine("{");
                var indent1 = ++indent;
                foreach (var (key, fieldValue) in json["Members"].Dict)
                {
                    CommandLineHelper.ToDictionary(fieldValue.TryGet("Args")?.Array.Select(v => (string)v), args);
                    strbuf.AppendComment(indent1, fieldValue, isCommentAttribute: isCommentAttribute);
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
                strbuf.WriteGeneratedFile(p, $"{cfgName}.CfgGen.cs");
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
            var json = ReadJson(Path.GetFullPath("./Declares.ts", p.SourceDirPath), p, out var strbuf);
            var indent = 1;
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in json["Members"].Dict)
            {
                CommandLineHelper.ToDictionary(item.Value.TryGet("Args")?.Array.Select(v => (string)v), args);
                if (args.ContainsKey("ignore"))
                    continue;
                Json5AnyValue fields;
                var commentAttribute = args.ContainsKey(nameof(CommentAttribute));
                switch ((string)item.Value["Type"])
                {
                    case "interface":
                        strbuf.AppendComment(indent, item.Value, isCommentAttribute: commentAttribute);
                        strbuf.Indent(indent).AppendLine("[BytesPackGen]");
                        strbuf.Indent(indent).Append("public partial ").Append(args.ContainsKey("class") ? "class" : "struct").Append(' ').Append(item.Key).Append(" {").AppendLine();
                        ++indent;
                        if (item.Value.TryGet("Fields", out fields))
                        {
                            foreach (var field in fields.Dict)
                            {
                                strbuf.AppendComment(indent, field.Value, isCommentAttribute: commentAttribute);
                                strbuf.Indent(indent).Append("[BytesPackGenField] ")
                                    .Append("public ").Append(field.Value["Type"].ToString()).Append(' ').Append(field.Key).Append(';').AppendLine();
                            }
                        }

                        --indent;
                        strbuf.Indent(indent).Append('}').AppendLine().AppendLine();

                        break;
                    case "enum":
                        strbuf.AppendComment(indent, item.Value, isCommentAttribute: commentAttribute);
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
                                strbuf.AppendComment(indent, field.Value, isCommentAttribute: commentAttribute);
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
            var jsonText = ConfigTypescriptHelper.Compile(fileName);
            strbuf = new StringBuilder(jsonText.Length).AppendFileHead(p, extraUsings);
            return Json5.Deserialize<Json5AnyValue>(jsonText);
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
        private static StringBuilder AppendComment(this StringBuilder strbuf, int indent, Json5AnyValue value, string key = "Comment", bool isCommentAttribute = false)
        {
            var text = value.TryGet(key)?.ToString();
            strbuf.Indent(indent).Append("/// <summary> ")
                .Append(text)
                .AppendLine(" </summary>");
            if (isCommentAttribute && text != null)
                strbuf.Indent(indent).Append("[Comment(\"").Append(text).AppendLine("\")]");
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