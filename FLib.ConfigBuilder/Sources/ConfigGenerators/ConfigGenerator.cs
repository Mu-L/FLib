// ==================== qcbf@qq.com | 2026-04-07 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib
{
    public static class ConfigGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        public static async Task Process(string sourceDirPath, string destDirPath, string ns = null)
        {
            destDirPath = Path.GetFullPath(destDirPath, sourceDirPath);
            FIO.ClearDirectory(destDirPath);
            
            var tasks = new ConcurrentBag<Task>()
            {
                ProcessDefines(Path.Combine(sourceDirPath, "_defines.json5"), Path.Combine(destDirPath, "_ConfigDefines.cs"), ns)
            };
            
            Directory.GetFiles(sourceDirPath, "*.schema.json5", SearchOption.AllDirectories).AsParallel().ForAll(jsonPath =>
                tasks.Add(ProcessConfig(jsonPath, destDirPath, ns)));
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public static async Task ProcessConfig(string jsonPath, string destDir, string ns)
        {
            var jsonText = await File.ReadAllTextAsync(jsonPath);
            var strbuf = new StringBuilder(jsonText.Length).AppendHead(ns);
            var json = Json5.Deserialize<Json5AnyValue>(jsonText);
            var indent = 1;
            var name = json["Name"].ToString();
            
            strbuf.Indent(indent).AppendLine("[BytesPackGen]")
                .Indent(indent).Append("public partial class ").Append(name).AppendLine(" {");
            
            
            strbuf.Indent(indent).AppendLine("}");
            if (ns != null)
                strbuf.Append('}');
            await File.WriteAllTextAsync(Path.Combine(destDir, $"{name}.cs"), strbuf.ToString());
        }
        
        /// <summary>
        /// 
        /// </summary>
        public static async Task ProcessDefines(string jsonPath, string destPath, string ns)
        {
            var jsonText = await File.ReadAllTextAsync(jsonPath);
            var strbuf = new StringBuilder(jsonText.Length).AppendHead(ns);
            var json = Json5.Deserialize<Json5AnyValue>(jsonText);
            var indent = 1;
            
            // 生成枚举
            foreach (var item in json["Enums"]!.CastDict)
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
                        strbuf.Indent(indent).Append(name).Append('=').Append(customValue.ToString()).Append(',').AppendLine();
                    else if (isFlags)
                        strbuf.Indent(indent).Append(name).Append('=').Append("1 << ").Append(i).Append(',').AppendLine();
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
                    strbuf.Indent(indent).AppendLine("/// <summary>");
                    strbuf.Indent(indent).Append("/// ").Append(item.Value.TryGet("Comment")?.ToString()).AppendLine();
                    strbuf.Indent(indent).AppendLine("/// </summary>");
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
                    strbuf.Indent(indent).Append('}').AppendLine();
                }
            }
            
            if (ns != null)
                strbuf.Append('}');
            
            await File.WriteAllTextAsync(destPath, strbuf.ToString());
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
        private static StringBuilder AppendHead(this StringBuilder strbuf, string ns)
        {
            strbuf.AppendLine("// generator sources")
                .AppendLine("using System;")
                .AppendLine("using FLib;");
            if (!string.IsNullOrEmpty(ns))
                strbuf.Append("namespace ").Append(ns).AppendLine().AppendLine("{");
            return strbuf;
        }
    }
}