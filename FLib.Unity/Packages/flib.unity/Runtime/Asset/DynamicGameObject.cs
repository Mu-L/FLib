//==================={By Qcbf|qcbf@qq.com|6/21/2022 10:48:46 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FLib.Unity
{
    public sealed class AssetLoaderGameObject : MonoBehaviour
    {
        public Action<AssetLoaderGameObject> OnLoadCompleteCallback;
        public AssetLoaderPath Path;


        public static AssetLoaderGameObject Create(in AssetLoaderPath path, Transform parent)
        {
            var go = new GameObject(
#if UNITY_EDITOR
                System.IO.Path.GetFileName(path)
#endif
            );
            go.transform.SetParent(parent, false);
            var loader = go.AddComponent<AssetLoaderGameObject>();
            loader.Path = path;
            AssetLoader.Load(go, path, loader.OnLoadComplete);
            return loader;
        }


        private void OnLoadComplete(AssetLoading.Result p)
        {
            p.Loaded.References.Add(gameObject);
            p.Loaded.Instantiate(transform);
            OnLoadCompleteCallback?.Invoke(this);
        }
    }
}
