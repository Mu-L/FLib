// ==================== qcbf@qq.com | 2026-04-07 ====================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FLib
{
    public static class ConfigGenerator
    {
        public static void Process(string sourceDirPath, string ns = null, bool isMultithread = true)
        {
            var codeSource = new StringBuilder(8192);
            codeSource.AppendLine("// generator sources");
            codeSource.AppendLine("using System;");
            codeSource.AppendLine("using FLib;");
            
            if (ns != null)
                codeSource.Append("namespace ").Append(ns).AppendLine().AppendLine("{");
            var bodyPos = codeSource.Length;
            ProcessDefines(codeSource, Path.Combine(sourceDirPath, "_defines.json5"));
            foreach (var filePath in Directory.GetFiles(sourceDirPath, "*", SearchOption.AllDirectories))
            {
            }
            
            if (ns != null)
                codeSource.AppendLine("}");
        }
        
        /// <summary>
        /// 
        /// </summary>
        public static void ProcessDefines(StringBuilder codeSource, string path)
        {
            var defines = Json5.Deserialize<Json5AnyValue>(File.ReadAllText(path));
            foreach (var item in defines["Enums"]!.AsDict!)
            {
                var isFlags = item.Value["IsFlags"];
                if (isFlags)
                    codeSource.AppendLine("[Flags]");
                codeSource.AppendLine($"public enum {item.Key} {{");
                var names = item.Value["Names"]!.AsArray!;
                var customValues = item.Value["Values"]!.AsDict;
                for (var i = 0; i < names.Length; i++)
                {
                    var name = names[i].ToString();
                    if (customValues?.TryGetValue(name, out var customValue) == true)
                        codeSource.Append(name).Append('=').Append(customValue.ToString()).AppendLine();
                    else if (isFlags)
                        codeSource.Append(name).Append('=').Append("1 << ").Append(i).Append(',').AppendLine();
                    else
                        codeSource.AppendLine(name);
                }
                
                codeSource.AppendLine("}");
            }
            
            foreach (var item in defines["Types"]!.AsDict!)
            {
                
            }
        }
    }
}