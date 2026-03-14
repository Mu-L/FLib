//==================={By Qcbf|qcbf@qq.com|11/5/2023 3:58:16 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FLib.Unity.Editor.PackBuilder;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEditor.EditorWindow;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class EditorFLibMenu
    {
        public const string MenuName = "Tools/FLib";
        public const int MenuPriority0 = 8000;
        public const int MenuPriority1 = 8010;

        #region Assets Tools
        [MenuItem("Assets/Tools/Open File", priority = -150)]
        public static void OpenFile()
        {
            foreach (var item in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(item);
                using var proc = Process.Start(Path.GetFullPath(path));
            }
        }

        [MenuItem("Assets/Reimport Selections", priority = 40)]
        public static void ReSerializeFile()
        {
            var paths = new List<string>();
            EditorFLibUtility.SelectionProcess<Object>(v =>
            {
                EditorUtility.SetDirty(v);
                paths.Add(AssetDatabase.GetAssetPath(v));
                return default;
            }).Forget();
            AssetDatabase.ForceReserializeAssets(paths);
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Tools/Copy Absolute Path", priority = -149)]
        public static void CopyAbsolutePath()
        {
            var targets = Selection.objects;
            var strbuf = StringFLibUtility.GetStrBuf();
            foreach (var target in targets)
                strbuf.Append(Path.GetFullPath(AssetDatabase.GetAssetPath(target))).Append('\n');
            strbuf.Remove(strbuf.Length - 1, 1);
            var str = StringFLibUtility.ReleaseStrBufAndResult(strbuf);
            EditorGUIUtility.systemCopyBuffer = str;
            Log.Info?.Write(str);
        }

        [MenuItem("Assets/Tools/Copy Path From GameRes #%&c", priority = -149)]
        public static void CopyGameResPath()
        {
            var targets = Selection.objects;
            var strbuf = StringFLibUtility.GetStrBuf();
            foreach (var target in targets)
                strbuf.Append(FIO.PathTrimLeftDirectory(AssetDatabase.GetAssetPath(target), 2)).Append('\n');
            strbuf.Remove(strbuf.Length - 1, 1);
            var str = StringFLibUtility.ReleaseStrBufAndResult(strbuf);
            EditorGUIUtility.systemCopyBuffer = str;
            Log.Info?.Write(str);
        }

        [MenuItem("Assets/Create/ScriptableObject")]
        public static void CreateScriptableObject()
        {
            TypeChooserWindow.Open(typeof(ScriptableObject), null, type =>
            {
                if (type == null) return;
                var obj = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(obj, FIO.SafePath(true, EditorFLibUtility.GetCurrentSelectionFolderPath() + $"/{CommentAttribute.TryGetLabel(type)}.asset"));
                Selection.activeObject = obj;
            }, type => !typeof(EditorWindow).IsAssignableFrom(type) && !typeof(UnityEditor.Editor).IsAssignableFrom(type), TypeChooserWindow.EOption.MustComment);
        }

        [MenuItem("Assets/Find Invert Dependencies", priority = 25)]
        public static void OpenFindInverseDependencies() => FindInverseDependencies.FindSelection();
        #endregion

        #region Menu Tools
        [MenuItem(MenuName + "/打开缓存目录", priority = MenuPriority1)]
        public static void OpenPersistentDirectory() => Process.Start(AssetLoader.PersistentPath);

        [MenuItem(MenuName + "/清除全部缓存", priority = MenuPriority1)]
        public static void ClearAllCaches()
        {
            FIO.ClearDirectory(AssetLoader.PersistentPath);
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("Assets/Create/2D/View Asset")]
        public static void CreateViewAsset()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var name = Path.GetFileNameWithoutExtension(path);
            path = FIO.GetFileDirectory(path) + "/" + name;
            var go = new GameObject("", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster))
            {
                hideFlags = HideFlags.HideInHierarchy,
                layer = LayerMask.NameToLayer("UI")
            };

            var canvas = go.GetComponent<Canvas>();
            canvas.vertexColorAlwaysGammaSpace = true;

            var rTransf = go.GetComponent<RectTransform>();
            rTransf.anchorMin = new Vector2(0, 0);
            rTransf.anchorMax = new Vector2(1, 1);
            rTransf.pivot = new Vector2(0.5f, 0.5f);
            rTransf.localPosition = Vector2.zero;
            rTransf.sizeDelta = Vector2.zero;

            path = FIO.SafePath(true, path + "View.prefab");
            go = PrefabUtility.SaveAsPrefabAsset(go, path);
            Selection.activeObject = go;
            AssetImporter.GetAtPath(path).SetLoadable(true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #region Menu Other Tools
        [MenuItem(MenuName + "/" + PackBuilder.UI.Stage.Name, priority = MenuPriority0)]
        public static void OpenBuilder() => GetWindow<PackBuilder.UI.Stage>(PackBuilder.UI.Stage.Name);

        [MenuItem(MenuName + "/Unity Internal Assets Finder", priority = MenuPriority1)]
        public static void OpenUnityInternalAssetFinder() => GetWindow<UnityInternalAssetFinder>();

        [MenuItem(MenuName + "/配置表工具", priority = MenuPriority0)]
        public static void OpenReloadConfigEditor() => GetWindow<ConfigToolEditor>("配置表工具");

        [MenuItem(MenuName + "/位图字体", priority = MenuPriority0)]
        public static void OpenBitmapFontWindow() => GetWindow<BitmapFontWindow>("位图字体");

#if !UNITY_6000_3_OR_NEWER
        [MenuItem(MenuName + "/Toolbar扩展工具", priority = MenuPriority0)]
        public static void ToolbarZoneExtender() => UnityToolbarZoneExtender.SwitchEnable();
#endif

        [MenuItem(MenuName + "/Log Level", priority = MenuPriority0)]
        public static void SetLogLevel()
        {
            var log = DialogWindow.Open(new DialogWindow.OptionData()
            {
                CustomUI = new EnumField((ELogLevel)PlayerPrefs.GetInt(nameof(ELogLevel), -1)),
                Btns = new[] { "Sure" },
            }, DialogWindow.EOpenType.ModalUtility).GetCustomUI<EnumField>(0);
            if (log != null)
            {
                var logLevel = (ELogLevel)log.value;
                if (logLevel == ELogLevel.None)
                    PlayerPrefs.DeleteKey(nameof(ELogLevel));
                else
                    PlayerPrefs.SetInt(nameof(ELogLevel), (int)logLevel);
                if (Application.isPlaying)
                {
                    if (logLevel == ELogLevel.None)
                        logLevel = ELogLevel.Info;
                    Log.Set(logLevel);
                }
            }
        }
        #endregion
        #endregion
    }
}
