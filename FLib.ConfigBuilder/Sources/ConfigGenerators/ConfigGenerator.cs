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

    public record ConfigGenerateParams(string SourceDirPath, string DestDirPath, string Namespace, EConfigGenerateOption Options = EConfigGenerateOption.Default, string[] Usings = null, string ConstConfigFileName = null)
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
        public static void Process(ConfigGenerateParams p)
        {
            Log.Info?.Write($"generate config {p}", nameof(ConfigGenerator));
            if (p.Op(EConfigGenerateOption.Clear))
            {
                foreach (var item in Directory.GetFiles(p.DestDirPath, "*.Gen.cs", SearchOption.TopDirectoryOnly))
                {
                    Log.Info?.Write($"remove config {item}", nameof(ConfigGenerator));
                    File.Delete(item);
                }
            }

            ProcessDefines(p);
            ProcessConstConfig(p);
            Directory.GetFiles(p.SourceDirPath, "*.schema.json5", SearchOption.AllDirectories).AsParallel().ForAll(jsonPath => ProcessConfig(jsonPath, p));
            OnGenerateProcess?.Invoke();
        }

        /// <summary>  </summary>
        public static void ProcessConstConfig(ConfigGenerateParams p)
        {
            if (string.IsNullOrEmpty(p.ConstConfigFileName))
                return;
            var path = Path.Combine(p.SourceDirPath, p.ConstConfigFileName + ".json5");
            if (!File.Exists(path))
                return;

            var json = ReadJson5(path, p, out var strbuf);
            if (json == null || json.Count == 0)
                return;

            var indent = 1;
            var name = json["Name"].ToString();
            Log.Info?.Write($"Process Const Config {name}", nameof(ConfigGenerator));
            strbuf.Indent(indent).Append("public static partial class ").Append(name).AppendLine().Indent(indent).AppendLine("{");
            ++indent;
            strbuf.AppendConfigSchemaFields(json["Fields"].Dict, indent, p, "static");
            --indent;
            strbuf.Indent(indent).AppendLine("}");
            WriteGeneratedFile(strbuf, p, $"{name}.Gen.cs");
        }

        /// <summary>  </summary>
        public static void ProcessConfig(string jsonPath, ConfigGenerateParams p)
        {
            try
            {
                var json = ReadJson5(jsonPath, p, out var strbuf);
                if (json == null || json.Count == 0)
                    return;
                var indent = 1;
                var name = json["Name"].ToString();
                var fileName = Path.GetFileNameWithoutExtension(jsonPath)[..^7];
                Log.Info?.Write($"Generate Config {name} {fileName}", nameof(ConfigGenerator));

                strbuf.AppendComment(indent, json, "Design")
                    .Indent(indent).AppendLine($"[BytesPackGen, Config(\"{fileName}\")]")
                    .Indent(indent).Append("public partial class ").Append(name).AppendLine().Indent(indent).AppendLine("{");
                ++indent;

                strbuf.AppendConfigSchemaFields(json["Fields"].Dict, indent, p);

                strbuf.Indent(indent).Append("public override string ToString() => ");
                var logFields = json["Fields"].Dict.Keys.Take(4).Select(k => $"Json5.SerializeToLog({k})").ToArray();
                if (logFields.Length > 0)
                    strbuf.Append("string.Join(\",\", new[] { ").Append(string.Join(", ", logFields)).AppendLine(" });").AppendLine();
                else
                    strbuf.AppendLine("string.Empty;").AppendLine();

                --indent;
                strbuf.Indent(indent).AppendLine("}");
                WriteGeneratedFile(strbuf, p, $"{name}.Gen.cs");
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
            var json = ReadJson5(Path.Combine(p.SourceDirPath, "_defines.json5"), p, out var strbuf);
            var indent = 1;

            // 生成枚举
            foreach (var item in json["Enums"].Dict)
            {
                Log.Info?.Write($"Generate Define Enum {item.Key}", nameof(ConfigGenerator));
                strbuf.AppendComment(indent, item.Value);
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
                        strbuf.AppendComment(indent, field.Value);
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
                    strbuf.AppendComment(indent, item.Value);
                    strbuf.Indent(indent).AppendLine("[BytesPackGen]");
                    strbuf.Indent(indent).Append("public partial ").Append(item.Value["Type"].ToString()).Append(' ').Append(item.Key).Append(" {").AppendLine();
                    ++indent;
                    if (item.Value.TryGet("Fields", out var fields))
                    {
                        foreach (var field in fields.Dict)
                        {
                            strbuf.AppendComment(indent, field.Value);
                            strbuf.Indent(indent).Append("[BytesPackGenField] ")
                                .Append("public ").Append(field.Value.ToString()).Append(' ').Append(field.Key).Append(';').AppendLine();
                        }
                    }

                    --indent;
                    strbuf.Indent(indent).Append('}').AppendLine().AppendLine();
                }
            }

            WriteGeneratedFile(strbuf, p, "_ConfigDefines.Gen.cs");
        }

        /// <summary>  </summary>
        private static Json5AnyValue ReadJson5(string path, ConfigGenerateParams p, out StringBuilder strbuf)
        {
            var jsonText = File.ReadAllText(path);
            strbuf = new StringBuilder(jsonText.Length).AppendHead(p);
            return Json5.Deserialize<Json5AnyValue>(jsonText);
        }

        /// <summary>  </summary>
        private static void WriteGeneratedFile(StringBuilder strbuf, ConfigGenerateParams p, string fileName)
        {
            if (p.HasNamespace)
                strbuf.Append('}');
            File.WriteAllText(Path.Combine(p.DestDirPath, fileName), strbuf.ToString());
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

        /// <summary>  </summary>
        private static StringBuilder AppendConfigSchemaFields(this StringBuilder strbuf, Dictionary<string, Json5AnyValue> fields, int indent, ConfigGenerateParams p, string modifier = null)
        {
            foreach (var (key, fieldValue) in fields)
            {
                strbuf.AppendComment(indent, fieldValue);
                strbuf.Indent(indent).Append("[BytesPackGenField] ").Append("public ");
                if (!string.IsNullOrEmpty(modifier))
                    strbuf.Append(modifier).Append(' ');
                strbuf.Append(fieldValue["Type"].ToString()).Append(' ').Append(key);
                if (p.Op(EConfigGenerateOption.UseProperty))
                    strbuf.Append(" { get; private set; }").AppendDefaultValue(fieldValue);
                else
                    strbuf.AppendDefaultValue(fieldValue)?.Append(';');

                strbuf.AppendLine().AppendLine();
            }

            return strbuf;
        }
    }
}