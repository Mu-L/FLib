//==================={By Qcbf|qcbf@qq.com|7/19/2021 12:03:50 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public static class EditorFLibUtility
    {
        /// <summary>
        /// 全部用户类型,经过一些不必要筛选过的
        /// </summary>
        public static readonly Type[] UserAssemblyTypes = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.IsDynamic).Select(asm =>
            new
            {
                Assembly = asm, asm.FullName,
                Matcher = new Regex(@"^(?:[Uu]nity|[Ss]ystem|EPPlus|dnlib|mscorlib|nunit|UniTask|MeshBaker|FairyGUI|log|Google|Mono|Hybrid|YooAsset|DOTween|Core|JetBrains|.*Json|ZString|Editor|PublicKeyToken|Bee\.|.*Debug|LZ4|.*Framework)")
            }).Where(v => !v.Matcher.Match(v.FullName).Success).SelectMany(v => v.Assembly.ExportedTypes).ToArray();

        /// <summary>
        /// 获取图片压缩尺寸
        /// </summary>
        public static readonly Func<Texture, long> GetTextureCompressedSize =
            (Func<Texture, long>)typeof(EditorWindow).Assembly.GetType("UnityEditor.TextureUtil").GetMethod("GetStorageMemorySizeLong")!
                .CreateDelegate(typeof(Func<Texture, long>));

        /// <summary>
        /// 剪切板
        /// </summary>
        public static string ClipboardTxt
        {
            get => EditorGUIUtility.systemCopyBuffer;
            set => EditorGUIUtility.systemCopyBuffer = value;
        }

        /// <summary>
        /// 清除控制台日志
        /// </summary>
        public static void ClearLog()
        {
            var logType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
            logType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
        }

        /// <summary>
        /// 裁剪路径
        /// </summary>
        public static string TrimToUnityAssetPath(string path)
        {
            if (path.StartsWith(Application.dataPath) || path.StartsWith(Application.dataPath.Replace('/', '\\')))
                path = path[(Application.dataPath.Length - 6)..];
            return path;
        }

        /// <summary>
        /// 裁剪到GameRes下一级
        /// </summary>
        public static string TrimToGameResPath(string path)
        {
            return FIO.PathTrimLeftDirectory(TrimToUnityAssetPath(path), 2);
        }

        /// <summary>
        /// 提示框
        /// </summary>
        public static void Alert(string msg)
        {
            EditorUtility.DisplayDialog("提示", msg, "确定");
        }

        /// <summary>
        /// 提示框
        /// </summary>
        public static bool AlertSure(string msg)
        {
            return EditorUtility.DisplayDialog("提示", msg, "确定", "取消");
        }

        /// <summary>
        /// 打开窗口到鼠标位置
        /// </summary>
        public static T WindowToCursor<T>(Vector2 screenPoint, T window, Vector2 offset = default) where T : EditorWindow
        {
            var resolution = Screen.currentResolution;
            var rect = window.position;
            var pos = screenPoint + offset;
            pos.x -= rect.width * 0.5f;
            rect.position = pos;
            if (rect.yMin >= resolution.height)
            {
                var diff = rect.yMin - resolution.width;
                rect.y -= diff;
            }
            else if (rect.yMax <= 26)
            {
                rect.y -= rect.yMax - 26;
            }

            window.position = rect;
            return window;
        }

        /// <summary>
        /// 打开窗口到鼠标位置
        /// </summary>
        public static T OpenWindowToPoint<T>(Vector2 point, Vector2 offset = default) where T : EditorWindow
        {
            return WindowToCursor(point, EditorWindow.GetWindow<T>(true), offset);
        }

        /// <summary>
        /// 打开窗口到鼠标位置
        /// </summary>
        public static T OpenWindowToCursor<T>(Vector2 offset = default) where T : EditorWindow
        {
            if (Event.current == null)
                return EditorWindow.GetWindow<T>(true);
            else
                return WindowToCursor(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), EditorWindow.GetWindow<T>(true), offset);
        }

        /// <summary>
        /// 是否为根GameObject
        /// </summary>
        public static bool IsPrefabRoot(GameObject go)
        {
#pragma warning disable UNT0008
            return PrefabUtility.IsAnyPrefabInstanceRoot(go) ||
                   PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot == go ||
                   PrefabUtility.GetOutermostPrefabInstanceRoot(go) == go ||
                   PrefabUtility.IsPartOfAnyPrefab(go);
        }

        /// <summary>
        /// 设置舞台自动保存
        /// </summary>
        public static void PrefabStageSetAutoSave(PrefabStage stage, bool value)
        {
            var prop = stage.GetType().GetProperty("autoSave", BindingFlags.NonPublic | BindingFlags.Instance);
            prop!.SetValue(stage, value);
        }

        /// <summary>
        /// 获取舞台自动保存
        /// </summary>
        public static bool PrefabStageGetAutoSave(PrefabStage stage)
        {
            var prop = stage.GetType().GetProperty("autoSave", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)prop!.GetValue(stage);
        }

        /// <summary>
        /// 舞台保存
        /// </summary>
        public static bool PrefabStageSave(PrefabStage stage)
        {
            if (!stage) return false;
            var prop = stage.GetType().GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)prop!.Invoke(stage, null);
        }

        /// <summary>
        /// 舞台保存
        /// </summary>
        public static bool PrefabStageSave()
        {
            return PrefabStageSave(PrefabStageUtility.GetCurrentPrefabStage());
        }

        /// <summary>
        /// 获取当前选择的文件夹路径
        /// </summary>
        public static string GetCurrentSelectionFolderPath()
        {
            string result;
            if (Selection.assetGUIDs.Length == 0)
            {
                result = "Assets";
            }
            else
            {
                result = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (!Directory.Exists(result))
                {
                    result = Path.GetDirectoryName(result);
                }
            }

            return result;
        }

        /// <summary>
        /// 处理当前选中物体 Prefab
        /// </summary>
        public static async UniTask SelectionPrefabProcess(Func<GameObject, UniTask> process)
        {
            await SelectionProcess<GameObject>(async obj =>
            {
                var assetType = PrefabUtility.GetPrefabAssetType(obj);
                if (assetType == PrefabAssetType.Regular || assetType == PrefabAssetType.Variant)
                {
                    try
                    {
                        await process(obj);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex, obj);
                    }
                }
            }, "*.prefab");
        }

        /// <summary>
        /// 处理当前选中物体 T
        /// </summary>
        public static async UniTask SelectionProcess<T>(Func<T, UniTask> process, string pattern = "*")
        {
            await AssetProcess(process, Selection.objects, pattern);
        }

        /// <summary>
        /// 处理资源
        /// </summary>
        public static async UniTask AssetProcess<T>(Func<T, UniTask> process, Object[] objects, string pattern = "*")
        {
            var t = typeof(T);
            var processType = 0;
            if (t == typeof(string))
                processType = 1;
            else if (typeof(AssetImporter).IsAssignableFrom(t))
                processType = 2;

            var objectsLength = objects.Length;
            using var progress = new EditorProgressBar();
            for (var i = 0; i < objectsLength; i++)
            {
                var obj = objects[i];
                if (obj is T target)
                {
                    if (processType == 0)
                    {
                        await Process(process, target);
                    }
                    if (processType == 1)
                    {
                        await Process(process, (T)(object)AssetDatabase.GetAssetPath(obj));
                    }
                    if (processType == 2)
                    {
                        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
                        if (importer != null)
                            await Process(process, (T)(object)importer);
                    }
                    if (progress.DisplayCancelable(obj.name, i / (float)objectsLength))
                    {
                        Log.Error?.Write("canceled");
                        return;
                    }
                }
                else if (obj is DefaultAsset)
                {
                    var folderPath = AssetDatabase.GetAssetPath(obj);
                    if (!Directory.Exists(folderPath))
                        continue;

                    var progressTitle = $"[{i / (float)objectsLength:p0}] {obj.name}";
                    var files = Directory.GetFiles(folderPath, pattern, SearchOption.AllDirectories);
                    var filesLength = files.Length;
                    for (var j = 0; j < filesLength; j++)
                    {
                        var filePath = files[j];
                        if (filePath.StartsWith('~'))
                            continue;
                        if (processType == 0)
                        {
                            await Process(process, (T)(object)AssetDatabase.LoadMainAssetAtPath(filePath));
                        }
                        if (processType == 1)
                        {
                            await Process(process, (T)(object)filePath);
                        }
                        if (processType == 2)
                        {
                            var importer = AssetImporter.GetAtPath(filePath);
                            if (importer != null)
                                await Process(process, (T)(object)importer);
                        }
                        if (progress.DisplayCancelable(progressTitle, filePath, j / (float)filesLength))
                        {
                            Log.Error?.Write("canceled");
                            return;
                        }
                    }
                }
            }
            return;

            static async UniTask Process(Func<T, UniTask> process, T target)
            {
                if (target != null)
                {
                    try
                    {
                        await process(target);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{target}\n{ex}");
                    }
                }
            }
        }

        /// <summary>
        ///  获取预制体源资源路径
        /// </summary>
        public static string GetPrefabAssetPath(GameObject target)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && target.scene == stage.scene)
                return stage.assetPath;
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            if (!string.IsNullOrEmpty(path))
                return path;
            return AssetDatabase.GetAssetPath(target);
        }

        /// <summary>
        /// 打开文件面板 
        /// </summary>
        public static string OpenFilePanel(string title, string folder, string[] filters)
        {
            var path = EditorUtility.OpenFilePanelWithFilters(title, PlayerPrefs.GetString(title, folder), filters);
            if (!string.IsNullOrEmpty(path))
                PlayerPrefs.SetString(title, FIO.GetFileDirectory(path));
            return path;
        }

        /// <summary>
        /// 打开文件面板 
        /// </summary>
        public static string OpenFilePanel(string title, string folder, string extension)
        {
            var path = EditorUtility.OpenFilePanel(title, PlayerPrefs.GetString(title, folder), extension);
            if (!string.IsNullOrEmpty(path))
                PlayerPrefs.SetString(title, FIO.GetFileDirectory(path));
            return TrimToUnityAssetPath(path);
        }

        /// <summary>
        /// 打开选择文件夹路径选择
        /// </summary>
        public static string OpenFolderPanel(string title, string folder, string defaultName)
        {
            var path = EditorUtility.OpenFolderPanel(title, PlayerPrefs.GetString(title, folder), defaultName);
            if (!string.IsNullOrEmpty(path))
                PlayerPrefs.SetString(title, FIO.GetFileDirectory(path));
            return TrimToUnityAssetPath(path);
        }

        /// <summary>
        /// 打开保存文件路径选择
        /// </summary>
        public static string SaveFilePanel(string title, string directory, string defaultName, string extension)
        {
            var path = EditorUtility.SaveFilePanel(title, PlayerPrefs.GetString(title, directory), defaultName, extension);
            if (!string.IsNullOrEmpty(path))
                PlayerPrefs.SetString(title, FIO.GetFileDirectory(path));
            return TrimToUnityAssetPath(path);
        }

        /// <summary>
        /// 打开保存文件夹路径选择
        /// </summary>
        public static string SaveFolderPanel(string title, string directory, string defaultName)
        {
            var path = EditorUtility.SaveFolderPanel(title, PlayerPrefs.GetString(title, directory), defaultName);
            if (!string.IsNullOrEmpty(path))
                PlayerPrefs.SetString(title, FIO.GetFileDirectory(path));
            return TrimToUnityAssetPath(path);
        }

        /// <summary>
        /// 获取指定场景的根对象
        /// </summary>
        public static List<GameObject> GetSceneAssetRootObjects(string sceneAssetPath)
        {
            var scene = SceneManager.GetSceneByPath(sceneAssetPath);
            var isUnloadable = !scene.IsValid();
            if (isUnloadable)
                scene = EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Additive);
            var list = new List<GameObject>(scene.rootCount);
            try
            {
                for (var i = 0; i < scene.rootCount; i++)
                    list.Add(scene.GetRootGameObjects()[i]);
            }
            finally
            {
                if (isUnloadable)
                    SceneManager.UnloadSceneAsync(scene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            }
            return list;
        }

        /// <summary>
        /// 退出匹配名称的场景
        /// </summary>
        public static int ExitAllScene(bool removeScene = true, Regex matchName = null, Regex excludeName = null, bool isExcludeFirst = true)
        {
            var count = SceneManager.sceneCount;
            var scenes = new List<Scene>();
            for (var i = isExcludeFirst ? 1 : 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (matchName?.Match(scene.name).Success != false && excludeName?.Match(scene.name).Success != false)
                    scenes.Add(scene);
            }
            foreach (var scene in scenes)
                EditorSceneManager.CloseScene(scene, removeScene);
            return scenes.Count;
        }

        /// <summary>
        /// 查找编辑器窗口 
        /// </summary>
        public static T FindEditorWindow<T>() where T : EditorWindow
        {
            return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
        }

        /// <summary>
        /// AssetDatabase.LoadAssetAtPath
        /// </summary>
        public static T LoadSubAsset<T>(string assetPath, char splitChar = ':') where T : Object
        {
            var index = assetPath.LastIndexOf(splitChar);
            if (index > 0)
            {
                var subAssetName = assetPath.AsSpan(index + 1);
                foreach (var item in AssetDatabase.LoadAllAssetsAtPath(assetPath[..index]).OfType<T>())
                {
                    if (subAssetName.Equals(item.name, StringComparison.Ordinal))
                        return item;
                }
            }
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
    }
}
