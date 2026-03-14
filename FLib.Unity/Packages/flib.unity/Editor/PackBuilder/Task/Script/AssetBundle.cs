// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public class AssetBundle : TaskBase
    {
        public bool IsCollectShaders = true;
        public static readonly string CacheDirectory = "UserSettings/zAssetBuildCache";

        public static readonly HashSet<string> ImageExtensions = new()
        {
            ".jpg", ".png", ".tga", ".bmp", ".cubemap", ".tif", ".psd", ".exr", ".dds", ".gif", ".hdr", ".webp", ".ico",
        };

        public static readonly Dictionary<string, int> BuildAssetSizes = new()
        {
            { AssetLoader.BUNDLE_MAIN_ASSET_NAME, 1024 * 20 },
            { ".png", 1024 * 10 },
            { ".jpg", 1024 * 10 },
            { ".tga", 1024 * 10 },
            { ".bmp", 1024 * 10 },
            { ".cubemap", 1024 * 10 },
            { ".fbx", 1024 * 10 },
            { ".anim", 1024 * 10 },
        };

        public static HashSet<string> BuildAssetTypes = ImageExtensions.Concat(new[]
        {
            ".prefab", ".obj", ".fbx", ".anim", ".asset", /*".mat",*/
            ".mp3", ".ogg", ".wav",
            ".ttf", ".otg"
        }).ToHashSet();


        public override void Execute(Context ctx)
        {
            ctx.Info.AssetMetas.Clear();
            if (!Directory.Exists(Utility.AssetCachePlatformAllPath))
            {
                Directory.CreateDirectory(Utility.AssetCachePlatformAllPath);
            }
            else if (!ctx.Schedule.Tasks.Any(v => v.UserInstance is PatchAssetBundle))
            {
                // 如果不是热更模式清空资源文件
                foreach (var item in Directory.GetFiles(Utility.AssetCachePlatformAllPath, "*", SearchOption.TopDirectoryOnly))
                {
                    if (!item.EndsWith(AssetLoader.INFO_FILE_NAME))
                        File.Delete(item);
                }

                foreach (var item in Directory.GetDirectories(Utility.AssetCachePlatformAllPath, "*", SearchOption.TopDirectoryOnly))
                    Directory.Delete(item, true);
            }

            var builds = GetFolderBuilds().Concat(GetAllBuilds(IsCollectShaders)).ToArray();
            const BuildAssetBundleOptions options = BuildAssetBundleOptions.DisableLoadAssetByFileName | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension |
                                                    BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.AssetBundleStripUnityVersion | BuildAssetBundleOptions.ChunkBasedCompression;
            var manifest = BuildPipeline.BuildAssetBundles(Utility.AssetCachePlatformAllPath, builds, options, Utility.Platform);
            if (manifest == null)
                throw new Exception("Build AssetBundles Failed");
            var allAssetPaths = manifest.GetAllAssetBundles();
            foreach (var assetPath in allAssetPaths)
            {
                ctx.GetInfoAssetMeta(assetPath) = new AssetLoaderInfo.Meta
                {
                    Hash = manifest.GetAssetBundleHash(assetPath),
                    Size = (int)new FileInfo(Path.Combine(Utility.AssetCachePlatformAllPath, assetPath)).Length,
                    Dependencies = manifest.GetAllDependencies(assetPath),
                };
            }
        }

        /// <summary>
        /// 获取所有需要打成AssetBundle包的文件夹资源
        /// </summary>
        public static IEnumerable<AssetBundleBuild> GetFolderBuilds()
        {
            var assetBundleNameBuffer = new string[1];
            var listBuffer1 = new List<string>(64);
            var listBuffer2 = new List<string>(64);
            return Directory.GetFiles(Utility.GameResFolder, Utility.LoadableFolderFileName, SearchOption.AllDirectories).Select(buildMakeFilePath =>
            {
                assetBundleNameBuffer[0] = Path.GetDirectoryName(buildMakeFilePath)!;
                GetAssets(File.ReadAllText(buildMakeFilePath));
                return new AssetBundleBuild
                {
                    addressableNames = listBuffer1.ToArray(),
                    assetNames = listBuffer2.ToArray(),
                    assetBundleName = GetAssetBundlePath(assetBundleNameBuffer[0])
                };
            });

            void GetAssets(string filter)
            {
                listBuffer1.Clear();
                listBuffer2.Clear();
                foreach (var item in AssetDatabase.FindAssets(filter, assetBundleNameBuffer))
                {
                    var path = AssetDatabase.GUIDToAssetPath(item);
                    if (File.Exists(path))
                    {
                        listBuffer1.Add(GetAssetBundlePath(path));
                        listBuffer2.Add(path);
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有需要打成AssetBundle包的资源
        /// </summary>
        public static IEnumerable<AssetBundleBuild> GetAllBuilds(bool isCollectShaders)
        {
            var addressableNames = new[] { AssetLoader.BUNDLE_MAIN_ASSET_NAME };
            var loadablePaths = AssetDatabase.GetAssetPathsFromAssetBundle(Utility.LoadableFlag);
            var buildPaths = new HashSet<string>(loadablePaths);
            var builds = new List<AssetBundleBuild>(loadablePaths.Length);
            var allDependencies = new SlimDictionary<string, int>();
            var fileSizeCached = new SlimDictionary<string, long>();
            var tileToAtlasPaths = new Dictionary<string, string>();
            var atlasToTilePaths = new Dictionary<string, string[]>();
            var shaders = isCollectShaders ? new HashSet<string>(1024) : null;

            AssetDatabase.ForceReserializeAssets(loadablePaths, ForceReserializeAssetsOptions.ReserializeAssets);

            // 收集图集，记录图集的散图和图集文件路径， 后续引用的只是散图，然后需要通过散图来反向查到图集文件作为同一个引用标记
            var searchInFolders = new[] { Utility.GameResFolder };
            foreach (var atlas in AssetDatabase.FindAssets("t:SpriteAtlas", searchInFolders))
            {
                var atlasPath = AssetDatabase.GUIDToAssetPath(atlas);
                var tilePaths = AssetDatabase.GetDependencies(atlasPath);
                atlasToTilePaths.Add(atlasPath, tilePaths);
                foreach (var atlasTilePath in tilePaths)
                {
                    if (File.Exists(atlasTilePath))
                        tileToAtlasPaths.Add(atlasTilePath, atlasPath);
                }
            }

            // 收集shader变体
            if (shaders != null)
            {
                foreach (var shaderVariant in AssetDatabase.FindAssets("t:ShaderVariantCollection", searchInFolders))
                    shaders.Add(AssetDatabase.GUIDToAssetPath(shaderVariant));
            }

            // 完整打包资源
            foreach (var loadablePath in loadablePaths)
            {
                if (!loadablePath.StartsWith(Utility.GameResFolder, StringComparison.Ordinal)) throw new Exception($"非游戏资源勾选了\"可动态加载\"{loadablePath}");
                AddBuild(loadablePath);
                foreach (var item in AssetDatabase.GetDependencies(loadablePath, true))
                {
                    if (shaders != null && (item.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) || item.EndsWith(".shaderGraph", StringComparison.OrdinalIgnoreCase)))
                    {
                        var shader = AssetDatabase.LoadAssetAtPath<Shader>(item);
                        if (!shader.name.StartsWith("TextMeshPro", StringComparison.OrdinalIgnoreCase))
                        {
                            shaders.Add(item);
                            continue;
                        }
                    }

                    var dependentPath = item;
                    if (!dependentPath.StartsWith(Utility.GameResFolder, StringComparison.Ordinal))
                        continue;

                    if (tileToAtlasPaths.TryGetValue(dependentPath, out var atlasPath))
                        dependentPath = atlasPath;

                    var isIgnoreSizeRule = false;
                    var pathExtension = Path.GetExtension(dependentPath).ToLowerInvariant();
                    if (buildPaths.Contains(dependentPath) || !BuildAssetTypes.Contains(pathExtension))
                    {
                        if (pathExtension == ".spriteatlasv2")
                            isIgnoreSizeRule = true;
                        else
                            continue;
                    }

                    if (++allDependencies.GetOrAddValueRef(dependentPath) == 2)
                    {
                        if (!isIgnoreSizeRule)
                        {
                            ref var fileSize = ref fileSizeCached.GetOrAddValueRef(dependentPath);
                            if (fileSize == 0)
                            {
                                if (ImageExtensions.Contains(pathExtension))
                                {
                                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(dependentPath);
                                    fileSize = EditorFLibUtility.GetTextureCompressedSize(tex);
                                }
                                else
                                {
                                    fileSize = new FileInfo(dependentPath).Length;
                                }
                            }

                            if (!BuildAssetSizes.TryGetValue(pathExtension, out var packSize))
                                packSize = BuildAssetSizes[AssetLoader.BUNDLE_MAIN_ASSET_NAME];

                            if (fileSize < packSize)
                                continue;
                            AddBuild(dependentPath);
                        }
                        else if (atlasToTilePaths.TryGetValue(dependentPath, out var atlasTiles))
                        {
                            buildPaths.Add(dependentPath);
                            builds.Add(new AssetBundleBuild
                            {
                                assetNames = atlasTiles,
                                assetBundleName = GetAssetBundlePath(dependentPath),
                            });
                        }
                        else
                        {
                            AddBuild(dependentPath);
                        }
                    }
                }
            }

            if (shaders?.Count > 0)
            {
                builds.Add(new AssetBundleBuild
                {
                    assetNames = shaders.ToArray(),
                    assetBundleName = AssetLoader.BUNDLE_SHADERS_ASSET_NAME,
                });
            }

            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
            return builds;

            void AddBuild(string path)
            {
                buildPaths.Add(path);
                var b = new AssetBundleBuild { assetNames = new[] { path }, assetBundleName = GetAssetBundlePath(path) };
                if (!path.EndsWith(".unity", StringComparison.Ordinal))
                    b.addressableNames = addressableNames;
                builds.Add(b);
            }
        }

        /// <summary>
        ///
        /// </summary>
        private static string GetAssetBundlePath(string assetPath)
        {
            return assetPath[Utility.GameResFolder.Length..].ToLowerInvariant();
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GetBuildsLog(IEnumerable<AssetBundleBuild> builds)
        {
            var strbuf = new StringBuilder(4096);
            var count = 0;
            foreach (var build in builds)
            {
                strbuf.Append(build.assetBundleName).AppendLine(":");
                foreach (var assetName in build.assetNames)
                    strbuf.Append(' ', 2).AppendLine(assetName);
                count++;
            }

            return $"builds[{count}]: \n{strbuf}";
        }
    }

    // public class ProcessShader : IPreprocessShaders
    // {
    //     public int callbackOrder { get; }
    //
    //     public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> datas)
    //     {
    //         var strbuf = StringFLibUtility.GetStrBuf();
    //         strbuf.Append($"{shader.name}/{snippet.passName}: ");
    //         foreach (var data in datas)
    //         {
    //             var keywords = data.shaderKeywordSet;
    //             foreach (var keyword in keywords.GetShaderKeywords())
    //                 strbuf.Append(keyword).Append(',');
    //         }
    //         Log.Info?.Write(StringFLibUtility.ReleaseStrBufAndResult(strbuf));
    //     }
    // }
}