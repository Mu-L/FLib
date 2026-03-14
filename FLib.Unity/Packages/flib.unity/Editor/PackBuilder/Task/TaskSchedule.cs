// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FLib.Unity.Editor.PackBuilder.Task.Script;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace FLib.Unity.Editor.PackBuilder.Task
{
    public class TaskSchedule
    {
        public bool IsDevelop;
        public bool IsDeepDevelop;
        public ScriptPack<TaskBase>[] Tasks;

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
            EditorUserBuildSettings.development = IsDevelop;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = IsDeepDevelop;

            if (!Application.isBatchMode)
            {
                foreach (var fontAsset in AssetDatabase.FindAssets("t:TMP_FontAsset"))
                {
                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontAsset));
                    if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                        font.ClearFontAssetData(true);
                }
            }

            var context = new Context(this);
            if (File.Exists(Utility.InfoPath))
                BytesPack.Unpack(ref context.Info, Compressor.Uncompress(File.ReadAllBytes(Utility.InfoPath)));
            foreach (var task in Tasks)
            {
                Log.Info?.Write($"Build: {task.UserInstance.Label}");
                task.UserInstance.Execute(context);
            }
            File.WriteAllBytes(Utility.InfoPath, context.GetInfoBytes());
            File.WriteAllBytes(Utility.InfoIdPath, BitConverter.GetBytes(context.Info.Id));
            foreach (var task in Tasks)
            {
                Log.Info?.Write($"LateBuild: {task.UserInstance.Label}");
                task.UserInstance.LateExecute(context);
            }
            foreach (var task in Tasks)
            {
                Log.Info?.Write($"FinishBuild: {task.UserInstance.Label}");
                task.UserInstance.FinishExecute(context);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Do(string json)
        {
            Json5.Deserialize<TaskSchedule>(json).Run();
        }
    }
}
