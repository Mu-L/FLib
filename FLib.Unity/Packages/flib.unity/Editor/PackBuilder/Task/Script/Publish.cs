// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public class Publish : TaskBase
    {
        public string Version;
        public bool IsIncrementVersion;
        public bool IsOpenOutputDir = false;
        public bool IsZip = false;
        public string ZipOutput;

        public string OutputPath { get; private set; }


        public override void LateExecute(Context context)
        {
            // 不知道为什么HybridCLR 执行AOT会修改这个设置为PVRTC
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;

            if (IsIncrementVersion)
                PlayerSettings.bundleVersion = (new FVersion(PlayerSettings.bundleVersion) + new FVersion(0, 0, 1)).ToString();
            else if (!string.IsNullOrEmpty(Version))
                PlayerSettings.bundleVersion = Version;

            OutputPath = $"{Utility.PublishPlatformPath}/{Application.productName}-{PlayerSettings.bundleVersion}";
            if (EditorUserBuildSettings.development)
                OutputPath += EditorUserBuildSettings.buildWithDeepProfilingSupport ? "-ddev" : "-dev";
            OutputPath += Utility.Platform switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => $"/{Application.productName}.exe",
                BuildTarget.Android => ".apk",
                BuildTarget.StandaloneOSX => ".app",
                BuildTarget.iOS or BuildTarget.WebGL => "",
                _ => throw new NotSupportedException(Utility.PublishPlatformPath)
            };

            if (File.Exists(OutputPath))
                File.Delete(OutputPath);

            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,
                OutputPath,
                Utility.Platform,
                Utility.GetBuildOption());

            var reportSummary = report.summary;
            OutputPath = reportSummary.outputPath;

            if (IsZip && Utility.Platform is BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64)
            {
                var dirPath = Path.GetDirectoryName(OutputPath);
                FIO.CreateZip(new[] { dirPath }, ZipOutput ?? dirPath + ".zip", new Regex("ButDontShipItWithYourGame|DoNotShip", RegexOptions.IgnoreCase));
            }

            Log.Info?.Write($"Pack Path: {OutputPath}");
            if (reportSummary.totalErrors > 0)
                throw new Exception($"publish error {report.SummarizeErrors()}");
            if (IsOpenOutputDir)
                Utility.OpenFolder(reportSummary.outputPath);
        }
    }
}
