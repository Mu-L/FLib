//==================={By Qcbf|qcbf@qq.com|5/9/2022 11:53:29 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FLib.Unity
{
    public struct AssetLoaderPath : IEquatable<AssetLoaderPath>
    {
        public string Value;
        public readonly bool IsEmpty => string.IsNullOrWhiteSpace(Value);
        public readonly string FullPath => GetFullPath(out _);

        public AssetLoaderPath(string relativePath)
        {
#if DEBUG
            if (string.IsNullOrEmpty(relativePath))
                throw new Exception("not found path");
#endif
            Value = relativePath.ToLowerInvariant();
        }

        public readonly string GetFullPath(out bool isBuiltin)
        {
            isBuiltin = true;
#if ASSET_BUNDLE || !UNITY_EDITOR
            var fileName = AssetLoader.Info.AssetMetas == null ? Value : AssetLoader.Info.AssetMetas[Value].FileNameStr;
#if UNITY_WEBGL
            return Path.Combine(AssetLoader.CDN, fileName);
#else
            var persistentPath = Path.Combine(AssetLoader.PersistentAssetPath, fileName);
            if (File.Exists(persistentPath))
            {
                isBuiltin = false;
                return persistentPath;
            }
            return Path.Combine(Application.streamingAssetsPath, AssetLoader.GAME_RES_NAME, fileName);
#endif
#else
            return Path.Combine("Assets", AssetLoader.GAME_RES_NAME, Value);
#endif
        }


        public readonly override string ToString()
        {
            return Value;
        }

        readonly bool IEquatable<AssetLoaderPath>.Equals(AssetLoaderPath other) => Value == other.Value;
        public readonly override int GetHashCode() => Value.GetHashCode();
        public readonly override bool Equals(object obj) => obj is AssetLoaderPath tmp && Value == tmp.Value;

        public static bool operator ==(in AssetLoaderPath a, in AssetLoaderPath b) => a.Value == b.Value;
        public static bool operator !=(in AssetLoaderPath a, in AssetLoaderPath b) => a.Value != b.Value;
        public static implicit operator AssetLoaderPath(string relativePath) => new(relativePath);
        public static implicit operator string(AssetLoaderPath relativePath) => relativePath.Value;
    }
}
