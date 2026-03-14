//==================={By Qcbf|qcbf@qq.com|12/1/2021 6:43:24 PM}===================

using Cysharp.Threading.Tasks;
using FLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    [StructLayout(LayoutKind.Auto)]
    public struct AssetLoading
    {
        public List<CompletionData> CompletionDatas;
        public AssetLoaderPath Path;
        public int LoadDependenciesCount;
        public AsyncOperation AsyncLoading;
        public AssetBundle Bundle;
        public object MainAsset;
        public bool IsAsyncLoadBundle;
        private float StartLoadTime;

        public readonly float ProgressValue => (AsyncLoading?.progress).GetValueOrDefault();

        public struct CompletionData
        {
            public AssetLoadReference Reference;
            public bool ReferenceIsValid;
            public object UserData;
            public CallbackData Call;
        }

        public struct CallbackData
        {
            public AutoResetUniTaskCompletionSource<AssetLoaded> FinishTaskCompletion;
            public Action<Result> FinishCallback;
            public ProgressDelegate ProgressCallback;
            public readonly override string ToString() => FinishCallback?.ToString() ?? FinishTaskCompletion.ToString();
        }

        public delegate void ProgressDelegate(in AssetLoading loading, int completionIndex);


        public struct Result
        {
            public object UserData;
            public AssetLoaded Loaded;
            public static implicit operator AssetLoaded(in Result p) => p.Loaded;
        }

        public readonly override string ToString() => Path;


        public bool Load()
        {
            if (AsyncLoading == null)
            {
                StartLoadTime = Time.time;
                AsyncLoading = ReqAsset(Path);
            }

            if (AsyncLoading.isDone)
            {
                MainAsset = GetAsset(AsyncLoading);
#if UNITY_EDITOR && !ASSET_BUNDLE
                if (!Path.Value.EndsWith(AssetLoader.NON_BUNDLE_EXTENSION))
                {
                    var fullPath = Path.FullPath;
                    MainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(fullPath);
                    if (MainAsset != null)
                    {
                        if (UnityEditor.AssetImporter.GetAtPath(fullPath).assetBundleName != "loadable")
                            Log.Error?.Write("资源未选择\"可动态加载\"，发布版将无法加载该资源\n" + Path);
                    }
                }
#endif
                Bundle = MainAsset as AssetBundle;
                if (Bundle != null && Bundle.Contains(AssetLoader.BUNDLE_MAIN_ASSET_NAME))
                {
                    if (IsAsyncLoadBundle)
                        AsyncLoading = Bundle.LoadAssetAsync(AssetLoader.BUNDLE_MAIN_ASSET_NAME);
                    else
                        MainAsset = Bundle.LoadAsset(AssetLoader.BUNDLE_MAIN_ASSET_NAME);
                }
                return true;
            }

            try
            {
                for (var i = CompletionDatas.Count - 1; i >= 0; i--)
                {
                    CompletionDatas[i].Call.ProgressCallback?.Invoke(this, i);
                }
            }
            catch (Exception ex)
            {
                Log.Error?.Write($"loading progress {Path}\n{ex}");
            }

            return StartLoadTime > 0 && Time.time - StartLoadTime > 15;
        }


        private static object GetAsset(in object loading)
        {
            if (loading is AssetBundleCreateRequest abLoading)
            {
                return abLoading.assetBundle;
            }
            if (loading is AssetBundleRequest abReq)
            {
                return abReq.asset;
            }
            if (loading is UnityWebRequestAsyncOperation webLoading)
            {
                try
                {
                    return webLoading.webRequest.downloadHandler is DownloadHandlerAssetBundle ab
                        ? ab.assetBundle
                        : webLoading.webRequest.downloadHandler.data;
                }
                finally
                {
                    webLoading.webRequest.Dispose();
                }
            }
            return null;
        }


        private static AsyncOperation ReqAsset(in AssetLoaderPath path)
        {
            UnityWebRequest req;
            var isRawFile = path.Value.EndsWith(AssetLoader.NON_BUNDLE_EXTENSION);
            var fullPath = path.GetFullPath(out var isBuiltin);
#if UNITY_EDITOR && !ASSET_BUNDLE
            isRawFile = true;
            fullPath = System.IO.Path.GetFullPath(fullPath);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
#endif
            if (isRawFile)
            {
#if UNITY_ANDROID
                if (!isBuiltin)
                    fullPath = "file:///" + fullPath;
#endif
                req = UnityWebRequest.Get(fullPath);
            }
            else
            {
#if UNITY_WEBGL
                req = UnityWebRequestAssetBundle.GetAssetBundle(fullPath);
                ((DownloadHandlerAssetBundle)req.downloadHandler).autoLoadAssetBundle = true;
#else
                return AssetBundle.LoadFromFileAsync(fullPath);
#endif
            }

            return req.SendWebRequest();
        }
    }
}
