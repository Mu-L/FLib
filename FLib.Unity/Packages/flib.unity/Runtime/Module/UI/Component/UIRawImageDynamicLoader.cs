//==================={By Qcbf|qcbf@qq.com|8/29/2022 11:51:15 AM}===================

using System;
using System.Collections.Generic;
using FLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FLib.Unity
{
    [RequireComponent(typeof(RawImage))]
    public class UIRawImageDynamicLoader : MonoBehaviour
    {
        public string DefaultPath;
        public RawImage Image;
        public UnityEvent LoadCompleteEvent;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(DefaultPath))
            {
                AssetLoader.Load(gameObject, DefaultPath, OnLoadComplete);
            }
        }

        public void Load(string path)
        {
            if (DefaultPath == path)
            {
                return;
            }

            if (DefaultPath != null)
            {
                AssetLoader.RemoveLoading(DefaultPath, OnLoadComplete);
            }

            DefaultPath = path;
            AssetLoader.Load(gameObject, path, OnLoadComplete);
        }

        private void OnLoadComplete(AssetLoading.Result obj)
        {
            if (obj.Loaded == null) return;
            Image.texture = (Texture)obj.Loaded.MainAsset;
            LoadCompleteEvent?.Invoke();
        }
    }
}
