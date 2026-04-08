// ==================== qcbf@qq.com | 2026-04-07 ====================

using System.Collections.Generic;
using System.IO;

namespace FLib
{
    public static class ConfigGenerator
    {
        public static void Process(string sourceDirPath, bool isMultithread = true)
        {
            ProcessDefines(Path.Combine(sourceDirPath, "_defines.json5"));
            foreach (var filePath in Directory.GetFiles(sourceDirPath, "*", SearchOption.AllDirectories))
            {
            }
        }
        
        public static void ProcessDefines(string path)
        {
            var defineJson = File.ReadAllText(path);
            
            
            
        }
    }
}