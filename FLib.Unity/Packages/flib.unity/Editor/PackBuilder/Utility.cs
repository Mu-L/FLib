// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FLib.Unity.Editor.PackBuilder
{
    public static class Utility
    {
        public const string LoadableFlag = "loadable";
        public const string LoadableFolderFileName = ".loadable";
        public static readonly string GameResFolder = $"Assets/{AssetLoader.GAME_RES_NAME}/";
        public static readonly GUIContent GUILoadableLabel = new("可动态加载");
        public static readonly GUILayoutOption[] GUILoadableLabelLayout = { GUILayout.ExpandWidth(false) };

        public static readonly BuildTarget Platform = EditorUserBuildSettings.activeBuildTarget;
        public static readonly string AssetCachePath = "zAssetBuildCache";
        public static readonly string AssetCachePlatformPath = $"{AssetCachePath}/{Platform}";
        public static readonly string AssetCachePlatformAllPath = $"{AssetCachePath}/{Platform}/all";
        public static readonly string AssetCachePlatformPatchesPath = $"{AssetCachePath}/{Platform}/patches";
        public static readonly string PublishPath = "Publish";
        public static readonly string PublishPlatformPath = $"{PublishPath}/{Platform}";
        public static readonly string InfoPath = $"{AssetCachePlatformAllPath}/{AssetLoader.INFO_FILE_NAME}";
        public static readonly string InfoIdPath = $"{AssetCachePlatformAllPath}/{AssetLoader.INFO_ID_FILE_NAME}";

        /// <summary>
        /// 
        /// </summary>
        public static void OpenFolder(string path)
        {
            Process.Start("explorer", "/select," + Path.GetFullPath(path.Replace('/', '\\')));
        }

        /// <summary>
        ///
        /// </summary>
        public static BuildOptions GetBuildOption()
        {
            var options = BuildOptions.None;
            if (EditorUserBuildSettings.development)
                options |= BuildOptions.Development;
            if (EditorUserBuildSettings.buildWithDeepProfilingSupport)
                options |= BuildOptions.EnableDeepProfilingSupport;
            return options;
        }

        /// <summary>
        ///
        /// </summary>
        public static bool IsLoadable(this AssetImporter importer)
        {
            if (Directory.Exists(importer.assetPath))
                return File.Exists(Path.Combine(importer.assetPath, LoadableFolderFileName));
            return importer.assetBundleName == LoadableFlag;
        }

        /// <summary>
        ///
        /// </summary>
        public static void SetLoadable(this AssetImporter importer, bool value)
        {
            if (Directory.Exists(importer.assetPath))
            {
                var path = Path.Combine(importer.assetPath, LoadableFolderFileName);
                if (value)
                    File.Create(path).Dispose();
                else
                    File.Delete(path);
            }
            else
            {
                importer.assetBundleName = value ? LoadableFlag : string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsAssetBundleMode
        {
            #region
            get =>
#if ASSET_BUNDLE
                true;
#else
                false;
#endif
            set
            {
                var buildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
                PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var defines);
                var isChange = false;
                if (value)
                {
                    if (Array.IndexOf(defines, "ASSET_BUNDLE") == -1)
                    {
                        isChange = true;
                        ArrayFLibUtility.Add(ref defines, "ASSET_BUNDLE");
                        PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
                    }
                }
                else
                {
                    if (Array.IndexOf(defines, "ASSET_BUNDLE") != -1)
                    {
                        ArrayFLibUtility.Remove(ref defines, "ASSET_BUNDLE");
                        if (defines.Length == 0)
                            PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.Empty);
                        else
                            PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
                        isChange = true;
                    }
                }
                if (isChange)
                    AssetDatabase.SaveAssets();
            }
            #endregion
        }
    }
}
