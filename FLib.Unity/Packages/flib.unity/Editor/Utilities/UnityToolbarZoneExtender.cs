// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AssetBundleBrowser;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class UnityToolbarZoneExtender : VisualElement
    {
#if !UNITY_6000_3_OR_NEWER
        public static void SwitchEnable()
        {
            if (EditorPrefs.HasKey(nameof(UnityToolbarZoneExtender)))
            {
                EditorPrefs.DeleteKey(nameof(UnityToolbarZoneExtender));
            }
            else
            {
                EditorPrefs.SetBool(nameof(UnityToolbarZoneExtender), true);
                Initialize();
            }
        }

        public static void Initialize()
        {
            if (!EditorPrefs.HasKey(nameof(UnityToolbarZoneExtender)))
                return;
            EditorApplication.update += DelayedInitialize;
            return;

            static void DelayedInitialize()
            {
                EditorApplication.update -= DelayedInitialize;
                var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
                var toolbarObj = toolbarType.GetField("get", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
                if (toolbarObj == null)
                    return;
                var toolbarRoot = (VisualElement)toolbarType.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(toolbarObj);
                var toolbarLeft = toolbarRoot.Q("ToolbarZoneLeftAlign");
                toolbarLeft.Clear();
                toolbarLeft.Add(new UnityToolbarZoneExtender());
            }
        }

        public UnityToolbarZoneExtender()
        {
            style.flexDirection = FlexDirection.RowReverse;
            style.flexGrow = 1;
            Add(CreateTimeScale());
            Add(CreateScenes());
        }

        /// <summary>
        /// 
        /// </summary>
        private VisualElement CreateTimeScale()
        {
            var btn = new ToolbarButton() { text = $"TS[{Time.timeScale}]", tooltip = "click set time scale 1" };
            btn.clicked += () => Set(1);
            btn.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 1) return;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("0.1"), false, () => Set(0.5f));
                menu.AddItem(new GUIContent("0.5"), false, () => Set(0.5f));
                menu.AddItem(new GUIContent("2"), false, () => Set(2f));
                menu.AddItem(new GUIContent("3"), false, () => Set(3f));
                menu.AddItem(new GUIContent("5"), false, () => Set(5f));
                menu.AddItem(new GUIContent("10"), false, () => Set(10f));
                menu.AddItem(new GUIContent("20"), false, () => Set(20f));
                menu.ShowAsContext();
                Event.current.Use();
            });
            return btn;

            void Set(float t)
            {
                if (t == 1)
                    UnityTimeScaler.Unset(nameof(UnityToolbarZoneExtender));
                else
                    UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), t, int.MaxValue);
                btn.text = $"TS[{t}]";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private VisualElement CreateScenes()
        {
            var btn = new ToolbarButton() { text = "Scenes" };
            btn.RegisterCallback<ClickEvent>(evt =>
            {
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
                if (evt.shiftKey || evt.ctrlKey)
                    EditorApplication.isPlaying = true;
            });
            btn.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 1) return;
                var menu = new GenericMenu();
                var basePath = $"Assets/{AssetLoader.GAME_RES_NAME}/";
                foreach (var path in AssetDatabase.FindAssets("t:Scene", new[] { basePath })
                             .Select(sceneGuid => AssetDatabase.GUIDToAssetPath(sceneGuid)[basePath.Length..].Replace('/', '>'))
                             .Order(v => v, true))
                {
                    menu.AddItem(new GUIContent(path), false, pathArg =>
                            EditorSceneManager.OpenScene(
                                basePath + ((string)pathArg).Replace('>', '/'), Keyboard.current.shiftKey.isPressed ? OpenSceneMode.Additive : OpenSceneMode.Single)
                        , path);
                }

                menu.ShowAsContext();
                Event.current.Use();
            });
            return btn;
        }
#else
        [MainToolbarElement("Time Scale", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement TimeScale() =>
            new MainToolbarDropdown(new MainToolbarContent((Texture2D)EditorGUIUtility.IconContent("d_Animation.LastKey").image), rect =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Revert"), false, () => UnityTimeScaler.Unset(nameof(UnityToolbarZoneExtender)));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("0.1"), false, () => UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), 0.5f));
                menu.AddItem(new GUIContent("0.5"), false, () => UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), 0.5f));
                menu.AddItem(new GUIContent("2"), false, () => UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), 2f));
                menu.AddItem(new GUIContent("3"), false, () => UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), 3f));
                menu.AddItem(new GUIContent("5"), false, () => UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), 5f));
                menu.AddItem(new GUIContent("10"), false, () => UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), 10f));
                menu.AddItem(new GUIContent("20"), false, () => UnityTimeScaler.Set(nameof(UnityToolbarZoneExtender), 20f));
                menu.DropDown(rect);
            });

        [MainToolbarElement("Scenes", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement Scenes()
        {
            return new MainToolbarDropdown(new MainToolbarContent((Texture2D)EditorGUIUtility.IconContent("UnityLogo").image), rect =>
            {
                var menu = new GenericMenu();
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    var path = scene.path;
                    menu.AddItem(new GUIContent(Path.GetFileNameWithoutExtension(path)), false, () => EditorSceneManager.OpenScene(path));
                }

                menu.AddSeparator("");
                var basePath = $"Assets/{AssetLoader.GAME_RES_NAME}/";
                foreach (var path in AssetDatabase.FindAssets("t:Scene", new[] { basePath })
                             .Select(sceneGuid => AssetDatabase.GUIDToAssetPath(sceneGuid)[basePath.Length..].Replace('/', '>'))
                             .Order(v => v, true))
                {
                    menu.AddItem(new GUIContent(path), false, pathArg =>
                            EditorSceneManager.OpenScene(
                                basePath + ((string)pathArg).Replace('>', '/'), Keyboard.current.shiftKey.isPressed ? OpenSceneMode.Additive : OpenSceneMode.Single)
                        , path);
                }

                menu.DropDown(rect);
            });
        }
#endif
    }
}