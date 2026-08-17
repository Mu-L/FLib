//==================={By Qcbf|qcbf@qq.com|9/20/2023 3:44:56 PM}===================

// #define ASSET_BUNDLE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FLib.Unity
{
    [DefaultExecutionOrder(1)]
    public abstract class FLibUnityLauncher : MonoBehaviour, IProgress<AssetSyncer.ProgressInfo>
    {
        public string GameLauncherTypeName = "GameLauncher";
        public string LaunchSettingPath = "zAssetConfigs/LaunchSetting.asset";

        protected virtual void Awake()
        {
            TryEnableLog();
            Launch().Forget();
        }

        protected virtual void OnDestroy()
        {
            try
            {
                ServiceMgr.Uninitialize();
            }
            catch (Exception e)
            {
                print(e.ToString());
            }
        }

        public async UniTask Launch()
        {
            InputBlocker.Timeout = -InputBlocker.Timeout;
            if (InputBlocker.Inst == null)
            {
                Log.Warn?.Write($"not found Component {nameof(InputBlocker)}");
                _ = new GameObject("[InputBlocker]", typeof(InputBlocker));
            }

            InputBlocker.Inst.OnTimeoutEvent = OnInputBlockerTimeout;
            try
            {
                var assetSyncer = new AssetSyncer();
#if ASSET_BUNDLE || !UNITY_EDITOR
                await assetSyncer.LoadLocalInfo();
#endif
                if (!string.IsNullOrEmpty(LaunchSettingPath))
                {
                    var loaded = await AssetLoader.Load(LaunchSettingPath);
                    var setting = loaded.GetMainAsset<FLibUnityLaunchSetting>();
                    loaded.IsUnloadAll = false;
                    loaded.Unload();
                    setting.Active();
                }
#if ASSET_BUNDLE || !UNITY_EDITOR
                await assetSyncer.DownloadCdnAssets(this);
#endif
            }
            catch (Exception e)
            {
                OnAssetSyncError(e);
                return;
            }

            LaunchProject().Forget();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void TryEnableLog()
        {
#if DEBUG
            if (PlayerPrefs.HasKey(nameof(ELogLevel)))
                Log.Set((ELogLevel)PlayerPrefs.GetInt(nameof(ELogLevel)));
#endif
#if UNITY_ANDROID
            const string path = "/sdcard/-LogLevel-";
            if (Directory.Exists(path))
            {
                var level = ELogLevel.Info;
                for (var i = ELogLevel.Verbose; i <= ELogLevel.Fatal; i++)
                {
                    if (Directory.Exists(path + i))
                    {
                        level = i;
                        break;
                    }
                }

                Log.Set(level);
            }
#endif
#if UNITY_EDITOR
            if (Log.IsEnableInfo)
            {
                FIO.ClearDirectory("logs/game");
                var logWriter = new LogFileWriter("Logs/game/out.log").Start();
                Application.logMessageReceivedThreaded += ((condition, trace, type) => logWriter.Logs.Add((Log.Info, type + condition + trace)));
            }
#endif
        }

        // private void OnApplicationQuit()
        // {
        //     ServiceMgr.End();
        //     AssetLoader.ClearAll();
        // }

        private void LateUpdate()
        {
            AssetLoader.Update();
        }

        void IProgress<AssetSyncer.ProgressInfo>.Report(AssetSyncer.ProgressInfo value) => OnAssetSyncProgress(value.Value, value.Label);

        protected virtual async UniTask LaunchProject()
        {
            try
            {
#if !UNITY_EDITOR && HYBRIDCLR
                await UniTask.WhenAll(
                    LoadDlls("aotdlls~", asmBytes => HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(asmBytes, HybridCLR.HomologousImageMode.Consistent)),
                    LoadDlls("hotdlls~", asmBytes => Assembly.Load(asmBytes))
                );
#endif
                InputBlocker.Open("...");
                var selfLaunchable = this as IUnityLaunchable;
                if (selfLaunchable != null)
                    await selfLaunchable.Prelaunch(this);

                InputBlocker.Open("....");
                var dynamicLauncherType = string.IsNullOrEmpty(GameLauncherTypeName) ? null : TypeAssistant.GetType(GameLauncherTypeName);
                object dynamicLauncher = null;
                if (dynamicLauncherType != null)
                    dynamicLauncher = typeof(MonoBehaviour).IsAssignableFrom(dynamicLauncherType) ? gameObject.AddComponent(dynamicLauncherType) : TypeAssistant.New(dynamicLauncherType);
                var dynamicLaunchable = dynamicLauncher as IUnityLaunchable;
                if (dynamicLaunchable != null)
                    await dynamicLaunchable.Prelaunch(this);

                InputBlocker.Open(".....");
                await UniTask.RunOnThreadPool(() => ObjectInjection.InjectAll());
                ServiceMgr.Initialize();
                InputBlocker.Timeout = Math.Abs(InputBlocker.Timeout);
                InputBlocker.CloseAll();

                if (selfLaunchable != null)
                    await selfLaunchable.Launch(this);
                if (dynamicLaunchable != null)
                    await dynamicLaunchable.Launch(this);
                // if (gameObject.TryGetComponent<LogViewer>(out var logViewer))
                //     logViewer.Refresh();
            }
            catch (Exception ex)
            {
                InputBlocker.Open(ex.Message);
                Log.Error?.Write(ex);
            }
        }

#if !UNITY_EDITOR && HYBRIDCLR
        private async UniTask LoadDlls(string typeName, Action<byte[]> process)
        {
            var hotDlls = AssetLoader.Info.AssetMetas.GetValueOrAdd(typeName).Dependencies;
            if (hotDlls != null)
            {
                foreach (var item in hotDlls)
                {
                    var loaded = await AssetLoader.Load(default, item);
                    AssetLoader.Info.AssetMetas.Remove(item);
                    Log.Info?.Write($"{typeName} load: {item}");
                    process(Compressor.Uncompress((byte[])loaded.MainAsset).ToArray());
                    loaded.Unload();
                }

                AssetLoader.Info.AssetMetas.Remove(typeName);
            }
            else
                Log.Info?.Write("no hot scripts");
        }
#endif


        public static async UniTask LoadConfig(string path)
        {
            try
            {
                var loaded = await AssetLoader.Load(path);
                ConfigHelper.DeserializeAll(((TextAsset)loaded.MainAsset).bytes, out _);
                loaded.Unload();
            }
            catch (Exception ex)
            {
                Log.Error?.Write(ex);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("ERROR", "Load Config Error", "ok");
#endif
            }
        }

        protected abstract void OnAssetSyncProgress(float progress, string label);
        protected abstract void OnAssetSyncError(Exception err);
        protected abstract void OnInputBlockerTimeout();
    }
}
