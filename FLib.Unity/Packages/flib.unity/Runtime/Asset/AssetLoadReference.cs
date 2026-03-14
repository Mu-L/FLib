//==================={By Qcbf|qcbf@qq.com|12/1/2021 7:09:02 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    public struct AssetLoadReference : IEquatable<AssetLoadReference>
    {
        public AssetLoaderPath Path;
        public object CustomReference;

        public bool IsValid
        {
            get
            {
                if (CustomReference != null)
                {
                    if (CustomReference as Object != null || (CustomReference as IAssetLoadReferenceable)?.IsAssetUsed == true) return true;
                }
                if (!Path.IsEmpty)
                {
                    if (AssetLoader.AssetLoadeds.ContainsKey(Path) || AssetLoader.AssetLoadingDict.ContainsKey(Path))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public AssetLoadReference(object custom)
        {
            Path = default;
            CustomReference = custom;
        }

        public AssetLoadReference(in AssetLoaderPath path, object custom)
        {
            Path = path;
            CustomReference = custom;
        }


        public readonly bool Equals(AssetLoadReference other)
        {
            return Path == other.Path && CustomReference == other.CustomReference;
        }

        public readonly override bool Equals(object obj)
        {
            return obj is AssetLoadReference r && r == this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path, CustomReference, IsValid);
        }

        public static implicit operator AssetLoadReference(in AssetLoaderPath v) => new() { Path = v };
        public static implicit operator AssetLoadReference(Object v) => new() { CustomReference = v };
        public static bool operator ==(in AssetLoadReference a, in AssetLoadReference b) => a.Path == b.Path && a.CustomReference == b.CustomReference;
        public static bool operator !=(in AssetLoadReference a, in AssetLoadReference b) => a.Path != b.Path && a.CustomReference != b.CustomReference;
    }
}
