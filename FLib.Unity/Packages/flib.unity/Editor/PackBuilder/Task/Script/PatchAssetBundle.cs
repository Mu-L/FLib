// ==================== qcbf@qq.com | 2025-07-01 ====================

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public class PatchAssetBundle : TaskBase
    {
        public static readonly string AssetCachePlatformPatchesIncrementPath = Utility.AssetCachePlatformPatchesPath + "-increment";
        
        public bool IsOpenOutputDir = false;
        
        public string PatchLog;
        public AssetLoaderInfo LocalInfo;
        // public override int GetPriority(WorldEntity entity, in WorldComponentHandleEx paramComp) => 10;


        public override void Execute(Context ctx)
        {
            var totalSize = 0L;
            var log = new StringBuilder();

            LocalInfo = AssetLoaderInfo.Unpack(File.ReadAllBytes(Utility.InfoPath));

            var patchPath = Path.Combine(Utility.AssetCachePlatformPatchesPath, ctx.Info.Id.ToString());
            FIO.ClearDirectory(patchPath);
            FIO.CreateDirectory(AssetCachePlatformPatchesIncrementPath);

            var validFileNames = new HashSet<string>(ctx.Info.AssetMetas.Count);
            foreach (var newAssetMeta in ctx.Info.AssetMetas)
            {
                validFileNames.Add(newAssetMeta.Value.FileNameStr);
                if (!LocalInfo.AssetMetas.TryGetValue(newAssetMeta.Key, out var localAssetMeta) || localAssetMeta != newAssetMeta.Value)
                {
                    var srcPath = Path.Combine(Utility.AssetCachePlatformAllPath, newAssetMeta.Key);
                    var assetSize = new FileInfo(srcPath).Length;
                    totalSize += assetSize;
                    log.AppendLine($"{newAssetMeta.Key} {FIO.FormatSize(assetSize)}");
                    CopyAsset.Copy(srcPath, patchPath, newAssetMeta.Value);
                    // if (!IsDontCopyIncrement)
                    {
                        File.Delete(Path.Combine(AssetCachePlatformPatchesIncrementPath, localAssetMeta.FileNameStr));
                        CopyAsset.Copy(srcPath, AssetCachePlatformPatchesIncrementPath, newAssetMeta.Value);
                    }
                }
            }

            if (totalSize > 0)
            {
                var infoFileBytes = ctx.GetInfoBytes();

                log.AppendLine($"{AssetLoader.INFO_FILE_NAME} {FIO.FormatSize(infoFileBytes.Length)}");
                totalSize += infoFileBytes.Length;

                File.WriteAllBytes(Path.Combine(patchPath, AssetLoader.INFO_FILE_NAME), infoFileBytes);
                // if (!IsDontCopyIncrement)
                {
                    foreach (var filePath in Directory.GetFiles(AssetCachePlatformPatchesIncrementPath, "*" + AssetLoader.BUNDLE_EXTENSION))
                    {
                        if (!validFileNames.Contains(Path.GetFileName(filePath)))
                            File.Delete(filePath);
                    }
                    File.WriteAllBytes(Path.Combine(AssetCachePlatformPatchesIncrementPath, AssetLoader.INFO_FILE_NAME), infoFileBytes);
                }
                if (IsOpenOutputDir)
                    Utility.OpenFolder(patchPath);
            }

            PatchLog = $"热更资源大小:{FIO.FormatSize(totalSize)}\n{log}";
        }

        public override void FinishExecute(Context context)
        {
            Log.Info?.Write(PatchLog);
        }
    }
}
