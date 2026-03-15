//=================================================={By Qcbf|qcbf@qq.com|11/16/2024 11:56:16 PM}==================================================

using System;
using System.IO;
using Cysharp.Threading.Tasks;
using FLib;
using FLib.Unity;
using K4os.Compression.LZ4;
using UnityEngine;

namespace Modules
{
    public class ProjectLauncher : FLibUnityLauncher
    {
        protected override void Awake()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_WEBGL)
            LZ4Codec.Enforce32 = true; // 因为在arm平台下，如果不设置这个，那么某些未对齐的字节可能出现初始化错误，本身0x0的字节可能变成0x32，导致错误 
#endif
            Launch().Forget();
        }

        protected override void OnAssetSyncError(Exception err)
        {
            InputBlocker.Open(err.ToString());
        }

        protected override void OnAssetSyncProgress(float progress, string label)
        {
            InputBlocker.Open($"[{progress:p1}]{label}");
        }

        protected override void OnInputBlockerTimeout()
        {
            Log.Error?.Write(nameof(OnInputBlockerTimeout));
        }
    }
}
