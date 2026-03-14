//==================={By Qcbf|qcbf@qq.com|10/25/2021 5:38:41 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class AssetDependenciesFinder
    {
        public static readonly HashSet<string> AllowAssetExtensions = new()
        {
            ".mat",".png",".tga",".jpg",".bmp",".json","xml",".spriteatlas",".controller",".shader",
            ".anim",".prefab",".hlsl",".asset",".txt",".otf",""
        };

        public Dictionary<string, HashSet<string>> AllReverseDependencies = new();
        public Dictionary<string, HashSet<string>> AllDependencies = new();

        public Condition[] Conditions = new Condition[0];


        public class Condition
        {
            public bool IsExclude = false;
            public string PathContains = "";
            public string StartsWith = "";
            public HashSet<string> ConditionExtensions = new();
            public HashSet<Type> ConditionTypes = new();

            public virtual bool IsValidPath(string path)
            {
                var tmp = path.StartsWith(StartsWith) && path.Contains(PathContains) &&
                          ConditionExtensions.Contains(Path.GetExtension(path));
                return IsExclude ? !tmp : tmp;
            }

            public virtual bool IsValidObject(Object obj)
            {
                var tmp = ConditionTypes.Contains(obj.GetType());
                return IsExclude ? !tmp : tmp;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Generate(Func<float, string, bool> progress)
        {
            try
            {
                AllDependencies.Clear();
                AllReverseDependencies.Clear();
                var allAssets = AssetDatabase.GetAllAssetPaths();
                var len = allAssets.Length;
                for (var i = 0; i < len; i++)
                {
                    var assetPath = allAssets[i];
                    if (!assetPath.StartsWith("Assets", StringComparison.Ordinal) ||
                        assetPath.StartsWith("Assets/Plugins", StringComparison.Ordinal) ||
                        !AllowAssetExtensions.Contains(Path.GetExtension(assetPath).ToLowerInvariant()))
                    {
                        continue;
                    }
                    var asset = IsValid(AssetDatabase.LoadMainAssetAtPath(IsValid(assetPath)));
                    if (asset == null)
                    {
                        continue;
                    }
                    var depAssetPaths = EditorUtility.CollectDependencies(new[] { asset });

                    if (progress?.Invoke(i / (float)len, assetPath) == true)
                    {
                        break;
                    }

                    if (!AllDependencies.TryGetValue(assetPath, out var deps))
                    {
                        deps = new HashSet<string>();
                        AllDependencies.Add(assetPath, deps);
                    }

                    foreach (var depAsset in depAssetPaths)
                    {
                        if (IsValid(depAsset) == null)
                        {
                            continue;
                        }
                        var depAssetPath = IsValid(AssetDatabase.GetAssetPath(depAsset));
                        if (depAssetPath == null)
                        {
                            continue;
                        }
                        if (depAssetPath == assetPath)
                        {
                            continue;
                        }

                        deps.Add(depAssetPath);

                        if (!AllReverseDependencies.TryGetValue(depAssetPath, out var revDeps))
                        {
                            revDeps = new HashSet<string>();
                            AllReverseDependencies.Add(depAssetPath, revDeps);
                        }
                        revDeps.Add(assetPath);
                    }
                }
                return true;
            }
            finally
            {
                EditorUtility.UnloadUnusedAssetsImmediate(true);
                GC.Collect();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public string IsValid(string assetPath)
        {
            if (assetPath == null)
            {
                return null;
            }
            foreach (var condition in Conditions)
            {
                if (!condition.IsValidPath(assetPath.ToLower()))
                {
                    return null;
                }
            }
            return assetPath;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public Object IsValid(Object asset)
        {
            if (asset == null)
            {
                return null;
            }
            foreach (var condition in Conditions)
            {
                if (!condition.IsValidObject(asset))
                {
                    return null;
                }
            }
            return asset;
        }



    }
}
