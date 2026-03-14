// =================================================={By Qcbf|qcbf@qq.com|2024-09-11}==================================================

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    public class FLibUnityLaunchSetting : ScriptableObject
    {
        public Object[] ExtraAssets;
        public string AssetCDN;

        public virtual void Active()
        {
            AssetLoader.CDN = AssetCDN;
        }
    }
}
