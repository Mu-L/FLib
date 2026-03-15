//=================================================={By Qcbf|qcbf@qq.com|11/17/2024 1:48:01 PM}==================================================

using System;
using Cysharp.Threading.Tasks;
using FLib;
using FLib.Net;
using FLib.Unity;
using Modules;
using Modules.Loading;
using UnityEngine;
using UnityEngine.Scripting;

namespace Launcher
{
    [Preserve]
    public class GameLauncher : IUnityLaunchable
    {
        public async UniTask Prelaunch(FLibUnityLauncher launcher)
        {
            if (Log.Info != null)
            {
                var loaded = await AssetLoader.Load("zGameSetting/DebugConsole/DebugConsole.prefab");
                loaded.Instantiate();
                loaded.IsUnloadAll = false;
                loaded.Unload();
            }

            UniTaskScheduler.UnobservedTaskException += exception => Log.Error?.Write(exception);
#if !UNITY_EDITOR
            Screen.SetResolution((int)(720 * (Screen.width / (float)Screen.height)), 720, true);
#endif
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate =
#if UNITY_EDITOR
                6000;
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
                60;
#endif
            Log.Info?.Write($"{nameof(GameLauncher)}, screen:{Screen.currentResolution}, fps:{Application.targetFrameRate}");

#if SERVER
            launcher.gameObject.AddComponent<UnityServer>();
#endif

            Initialize();
        }

        public async UniTask Launch(FLibUnityLauncher launcher)
        {
            await UniTask.WhenAll(
#if !UNITY_EDITOR || ASSET_BUNDLE
                AssetLoader.Load(AssetLoader.BUNDLE_SHADERS_ASSET_NAME).ContinueWith(AssetLoader.MoveToConstAsset),
#endif
                AssetLoader.Load("zGameSetting/AudioInitializer.prefab").ContinueWith(loaded => loaded.InstantiateAndUnloadBundle()),
                FLibUnityLauncher.LoadConfig("zGameSetting/cfg.bytes")
            );

#if !UNITY_EDITOR || ASSET_BUNDLE
            foreach (var shaderVariant in AssetLoader.ConstAssetLoadeds["shaders"].Bundle.LoadAllAssets<ShaderVariantCollection>())
                shaderVariant.WarmUp();
#endif
            // ModuleStage.OnStageProgress = progress => LoadingUI.Show(progress);
            // ModuleStage.Goto((int)EModuleStage.Login);
        }

        public static void Initialize()
        {
            TypeAssistant.AddAssemblies(typeof(GameLauncher).Assembly, typeof(Vector2).Assembly);
        }
    }
}