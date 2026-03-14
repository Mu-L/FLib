//==================={By Qcbf|qcbf@qq.com|7/14/2021 11:16:33 AM}===================

using Cysharp.Threading.Tasks;
using FLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    public class AssetLoaded
    {
        public AssetLoaderPath Path;
        public object MainAsset;
        public AssetBundle Bundle;
        public readonly List<AssetLoadReference> References = new();
        public float LastActiveTime;
        public float CacheTime = 2;
        public bool IsUnloadAll = true;

        public Scene LoadedScene { get; private set; }
        public override string ToString() => Path;

        public AssetLoaded Initialize()
        {
            LastActiveTime = Time.time;
            return this;
        }

        public T GetMainAsset<T>() where T : class
        {
            return MainAsset as T;
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        public async UniTask LoadScene(LoadSceneMode mode = LoadSceneMode.Additive, bool isSetActive = true)
        {
            string path;
#if !UNITY_EDITOR || ASSET_BUNDLE
            Log.Assert(Bundle.isStreamedSceneAssetBundle)?.Write($"{Path} not is scene");
            path = Bundle.GetAllScenePaths().Single();
            await SceneManager.LoadSceneAsync(path, mode);
#else
            path = $"Assets/GameRes/{Path}";
            await UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(mode));
#endif

            LoadedScene = SceneManager.GetSceneByPath(path);
            if (isSetActive)
                SceneManager.SetActiveScene(LoadedScene);
        }

        /// <summary>
        /// 创建一个T实例
        /// </summary>
        public T Instantiate<T>(Transform parent = null, bool instantiateInWorldSpace = false) where T : Object
        {
            if (typeof(Component).IsAssignableFrom(typeof(T)))
                return ((GameObject)Instantiate(parent, instantiateInWorldSpace)).GetComponent<T>();
            if (typeof(T) == typeof(Transform))
                return (T)(object)((GameObject)Instantiate(parent, instantiateInWorldSpace)).transform;
            return (T)Instantiate(parent, instantiateInWorldSpace);
        }

        /// <summary>
        /// 创建一个Object实例
        /// </summary>
        public Object Instantiate(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            var inst = Object.Instantiate((Object)MainAsset, parent, instantiateInWorldSpace);
            References.Add(inst);
            return inst;
        }

        /// <summary>
        /// 创建一个Object实例并且释放bundle资源
        /// </summary>
        public Object InstantiateAndUnloadBundle(Transform parent = null, bool isFixName = false, bool instantiateInWorldSpace = false)
        {
            var inst = Object.Instantiate((Object)MainAsset, parent, instantiateInWorldSpace);
            IsUnloadAll = false;
            Unload();
            if (isFixName && inst.name.EndsWith(')'))
                inst.name = inst.name[..^7];
            return inst;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Unload()
        {
            // Log.Info?.Write(Path, "Asset Unload", 0x666666);
            if (LoadedScene.handle != 0 && LoadedScene.IsValid())
                SceneManager.UnloadSceneAsync(LoadedScene, IsUnloadAll ? UnloadSceneOptions.UnloadAllEmbeddedSceneObjects : UnloadSceneOptions.None);
// #if !UNITY_EDITOR || ASSET_BUNDLE
//             if (MainAsset is GameObject go)
//                 Object.DestroyImmediate(go);
// #endif
            MainAsset = null;
            if (Bundle == null) return;
            Bundle.Unload(IsUnloadAll);
            Bundle = null;
        }
    }
}
