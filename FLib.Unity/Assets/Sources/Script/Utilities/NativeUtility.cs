//==================={By Qcbf|qcbf@qq.com|2/13/2023 5:44:16 PM}===================

// #define UNITY_ANDROID
// #undef UNITY_EDITOR

using System;
using System.Collections.Generic;
using FLib;
using UnityEngine;

// using WeChatWASM;

namespace Utilities
{
    public static class NativeUtility
    {
#if UNITY_ANDROID
        private static readonly AndroidJavaClass Utility = new("com.qwabc.common.Utility");
#endif

        #region Vibration
#if UNITY_ANDROID
        private static readonly object[] VibrationParams = { 15, 10 };
#endif

        public enum EVibrationLevel
        {
            Light,
            Medium,
            Heavy,
        }


        public static string GetAppHash()
        {
#if UNITY_EDITOR || UNITY_WEBGL
            return string.Empty;
#else
            return Utility.CallStatic<string>("GetAppHash")?.ToUpper();
#endif
        }


        public static void Vibrate(EVibrationLevel level)
        {
#if UNITY_EDITOR
            return;
#pragma warning disable CS0162 // Unreachable code detected
#endif
            if (level == EVibrationLevel.Light)
            {
#if UNITY_ANDROID
                VibrationParams[0] = 12;
                VibrationParams[1] = 80;
                Utility.CallStatic("Vibrate", VibrationParams);
#elif UNITY_WEBGL
                WX.VibrateShort(new VibrateShortOption { type = "light" });
#endif
            }
            else if (level == EVibrationLevel.Medium)
            {
#if UNITY_ANDROID
                VibrationParams[0] = 25;
                VibrationParams[1] = 190;
                Utility.CallStatic("Vibrate", VibrationParams);
#elif UNITY_WEBGL
                WX.VibrateShort(new VibrateShortOption { type = "medium" });
#endif
            }
            else if (level == EVibrationLevel.Heavy)
            {
#if UNITY_ANDROID
                VibrationParams[0] = 30;
                VibrationParams[1] = 255;
                Utility.CallStatic("Vibrate", VibrationParams);
#elif UNITY_WEBGL
                WX.VibrateShort(new VibrateShortOption { type = "heavy" });
#endif
            }
        }
        #endregion
    }
}
