// ==================== qcbf@qq.com | 2025-09-24 ====================

using System;
using System.Linq;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    [Comment("标记设置")]
    public class BitFlagsSetting : ScriptableObject
    {
        public GroupFlags[] Groups = Array.Empty<GroupFlags>();

        [Serializable]
        public struct GroupFlags
        {
#if UNITY_EDITOR
            public string Comment;
#endif
            public string[] FlagNames;
        }

        private void OnEnable() => BitFlags.Initialize(Groups.Select(g => g.FlagNames).ToArray());

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorLoad()
        {
            if (BitFlags.FlagGroupNames != null && UnityEditor.SessionState.GetBool(nameof(BitFlagsSetting), false)) return;
            foreach (var assetGuid in UnityEditor.AssetDatabase.FindAssets("t:WorldFlagsSetting", new[] { "Assets/" + AssetLoader.GAME_RES_NAME }))
                UnityEditor.AssetDatabase.LoadAssetByGUID<BitFlagsSetting>(new UnityEditor.GUID(assetGuid));
            UnityEditor.SessionState.SetBool(nameof(BitFlagsSetting), true);
        }

        private void OnValidate() => OnEnable();
#endif
    }
}