// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FLib.Unity.Editor.PackBuilder.Task;

namespace FLib.Unity.Editor.PackBuilder
{
    public static class CommandLine
    {
        /// <summary>
        /// 
        /// </summary>
        public static void Do()
        {
            var args = Environment.GetCommandLineArgs();
            Log.Info?.Write(args);
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals("--json", StringComparison.OrdinalIgnoreCase))
                {
                    Json5.Deserialize<TaskSchedule>(args[i + 1]).Run();
                    break;
                }
                if (arg.Equals("--jsonpath", StringComparison.OrdinalIgnoreCase))
                {
                    Json5.Deserialize<TaskSchedule>(File.ReadAllText(Path.GetFullPath(args[i + 1]))).Run();
                    break;
                }
            }
        }
    }
}
