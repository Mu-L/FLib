// ==================== qcbf@qq.com | 2025-08-29 ====================

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FLib;
using FLib.Unity;
using UnityEngine;
using Utilities;

namespace Modules.Prefs
{
    [ModuleService]
    public class PrefsService : StageServiceBase<PrefsService>
    {
        public override uint StageIdMask => (uint)EModuleStage.Logined;
        public static string KeyPrefix = string.Empty;

        public override UniTask OnEnterStage()
        {
            KeyPrefix = "login user"; // LoginService.Player.Uid.ToString();
            return default;
        }

        public override UniTask OnExitStage()
        {
            KeyPrefix = string.Empty;
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        public static T Get<T>(string key, bool isServer = false)
        {
            key = KeyPrefix + key;
            if (isServer)
                throw new NotSupportedException();
            return Json5.Deserialize<T>(PlayerPrefs.GetString(key));
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Set(string key, object value, bool isServer = false)
        {
            key = KeyPrefix + key;
            if (isServer)
                throw new NotSupportedException();
            if (value == null)
                PlayerPrefs.DeleteKey(key);
            else
                PlayerPrefs.SetString(key, Json5.Serialize(value));
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool Exist(string key, bool isServer = false)
        {
            key = KeyPrefix + key;
            if (isServer)
                throw new NotSupportedException();
            return PlayerPrefs.HasKey(key);
        }
    }
}