// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public class CopyAsset : TaskBase
    {
        public string ToPath = Application.streamingAssetsPath;

        // public override int GetPriority(WorldEntity entity, in WorldComponentHandleEx paramComp) => 100;

        public override void LateExecute(Context ctx)
        {
            if (string.IsNullOrEmpty(ToPath))
                throw new Exception("not found output path");

            Copy(Utility.AssetCachePlatformAllPath, Path.Combine(ToPath, AssetLoader.GAME_RES_NAME));
            AssetDatabase.Refresh();
        }

        public static void Copy(string src, string dst)
        {
            FIO.ClearDirectory(dst);
            File.Copy(Path.Combine(src, AssetLoader.INFO_FILE_NAME), Path.Combine(dst, AssetLoader.INFO_FILE_NAME), true);
            File.Copy(Path.Combine(src, AssetLoader.INFO_ID_FILE_NAME), Path.Combine(dst, AssetLoader.INFO_ID_FILE_NAME), true);
            var info = AssetLoaderInfo.Unpack(File.ReadAllBytes(Path.Combine(src, AssetLoader.INFO_FILE_NAME)));
            foreach (var item in info.AssetMetas)
            {
                if (!item.Key.EndsWith('~'))
                    Copy(Path.Combine(src, item.Key), dst, item.Value);
            }
        }

        public static void Copy(string srcFullPath, string dst, in AssetLoaderInfo.Meta meta)
        {
            var dstFullPath = Path.Combine(dst, meta.FileNameStr);
            //FIO.CreateDirectory(Path.GetDirectoryName(dstFullPath));
            File.Copy(srcFullPath, dstFullPath, true);
        }
    }
}
