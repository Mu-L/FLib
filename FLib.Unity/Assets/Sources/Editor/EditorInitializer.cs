// =================================================={By Qcbf|qcbf@qq.com|2024-04-22}==================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Configs;
using FLib;
using FLib.Unity;
using FLib.Unity.Editor;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Launcher
{
    public class EditorInitializer
    {
        [InitializeOnLoadMethod]
        public static void InitializeSystem()
        {
            SetupBasic();
            SetupConfig();
            UnitySerializationSupport();
            EditorApplication.focusChanged += EditorApplicationOnFocusChanged;
        }

        private static void SetupBasic()
        {
            TypeAssistant.AddAssemblies(typeof(EditorInitializer).Assembly);
            GameLauncher.Initialize();
            InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true); // if no flag, unity new input system cannot step frame debug
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            ObjectInjection.InjectAll();
        }

        /// <summary>
        /// 
        /// </summary>
        private static void SetupConfig()
        {
            if (string.IsNullOrEmpty(ConfigToolEditor.ConfigSourcePath))
                ConfigToolEditor.ConfigSourcePath = @"..\Config";
            ConfigBuilder.Sign = 'c';
            ConfigBuilder.OutputPath = "Assets/GameRes/zAssetConfigs/cfg.bytes";
        }

        /// <summary>
        /// 
        /// </summary>
        private static void UnitySerializationSupport()
        {
            Json5.CustomDeserializers ??= new Dictionary<Type, IJson5Deserializable>();
            Json5.CustomDeserializers.Add(typeof(NoiseSettings.TransformNoiseParams), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                if (nodes[0].Token != EJson5Token.ArrayOpen)
                    return false;
                var values = nodes.To<float[][]>();
                var r = new NoiseSettings.TransformNoiseParams();
                Parse(ref r.X, values.ElementAtOrDefault(0));
                Parse(ref r.Y, values.ElementAtOrDefault(1));
                Parse(ref r.Z, values.ElementAtOrDefault(2));
                return new Json5CustomDeserializeResult(r);

                static void Parse(ref NoiseSettings.NoiseParams p, float[] values)
                {
                    if (values == null)
                        return;
                    p.Frequency = values.ElementAtOrDefault(0);
                    p.Amplitude = values.ElementAtOrDefault(1);
                    p.Constant = values.ElementAtOrDefault(2) != 0;
                }
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        private static void EditorApplicationOnFocusChanged(bool val)
        {
            if (val || Application.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() != null /*PrefabStageUtility.GetCurrentPrefabStage()?.scene.isDirty == true*/)
                return;
            var startPath = $"Assets/{AssetLoader.GAME_RES_NAME}";
            foreach (var assetGuid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (!assetPath.StartsWith(startPath))
                    return;
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                if (font.atlasPopulationMode != AtlasPopulationMode.Static)
                {
                    font.ClearFontAssetData(true);
                    AssetDatabase.SaveAssetIfDirty(font);
                }
            }
        }
    }
}
