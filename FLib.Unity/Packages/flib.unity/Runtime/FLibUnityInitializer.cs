//==================={By Qcbf|qcbf@qq.com|10/10/2022 3:40:14 PM}===================

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public static class FLibUnityInitializer
    {
        private static LogFileWriter _logWriter;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
        public static void InitializeSystem()
        {
            Log.GlobalOutputHandler += OnLogOutputEvent;
            TypeAssistant.AddAssemblies(typeof(FLibUnityInitializer).Assembly);
        }

        [HideInCallstack]
        private static void OnLogOutputEvent(Log log, string text)
        {
            switch (log.Level)
            {
                case ELogLevel.Verbose:
                case ELogLevel.Debug:
                    var strbuf = StringFLibUtility.GetStrBuf(text.Length + 32);
                    strbuf.Append("<color=#").Append("777777").Append('>').Append(text).Append("</color>");
                    Debug.Log(StringFLibUtility.ReleaseStrBufAndResult(strbuf));
                    break;
                case ELogLevel.Info:
                    Debug.Log(text);
                    break;
                case ELogLevel.Warn:
                    Debug.LogWarning(text);
                    break;
                case ELogLevel.Error:
                case ELogLevel.Fatal:
                    Debug.LogError(text);
                    break;
            }
        }
    }
}
