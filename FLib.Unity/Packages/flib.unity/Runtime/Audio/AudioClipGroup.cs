// =================================================={By Qcbf|qcbf@qq.com|2024-10-30}==================================================

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FLib.Unity
{
    [Serializable]
    public class AudioClipGroup
    {
        public EType Type;
        public Data[] Datas;

        [Serializable]
        public struct Data
        {
            public AudioClip Clip;
            public string Path;
            public float Weight;
        }

        public enum EType
        {
            Random,
            Sequence,
        }

        public int NextDataIndex()
        {
            switch (Type)
            {
                case EType.Random:
                    break;
                case EType.Sequence:
                    break;
            }
            return -1;
        }

        public async UniTask<AudioClip> GetClip()
        {
            var index = NextDataIndex();
            return Datas[index].Clip ??= (await AssetLoader.Load(Datas[index].Path)).GetMainAsset<AudioClip>();
        }
    }
}
