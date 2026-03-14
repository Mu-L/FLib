//==================={By Qcbf|qcbf@qq.com|12/11/2023 8:54:25 PM}===================

using Cysharp.Threading.Tasks;
using FLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class ConfigToolEditor : EditorWindow
    {
        public FileSystemWatcher AutoLoadWatcher;
        private int _autoLoadVersion;

        public static bool IsMultithread
        {
            get => PlayerPrefs.GetInt(nameof(ConfigToolEditor) + nameof(IsMultithread), 1) == 1;
            set => PlayerPrefs.SetInt(nameof(ConfigToolEditor) + nameof(IsMultithread), value ? 1 : 0);
        }

        public static string ConfigSourcePath
        {
            get => PlayerPrefs.GetString(nameof(ConfigToolEditor) + nameof(ConfigSourcePath));
            set => PlayerPrefs.SetString(nameof(ConfigToolEditor) + nameof(ConfigSourcePath), value);
        }

        public static bool IsLaunchRebuild
        {
            get => PlayerPrefs.GetInt(nameof(ConfigToolEditor) + nameof(IsLaunchRebuild), 0) == 1;
            set => PlayerPrefs.SetInt(nameof(ConfigToolEditor) + nameof(IsLaunchRebuild), value ? 1 : 0);
        }

        public static int AutoRebuild
        {
            get => PlayerPrefs.GetInt(nameof(ConfigToolEditor) + nameof(AutoRebuild), 0);
            set
            {
                PlayerPrefs.SetInt(nameof(ConfigToolEditor) + nameof(AutoRebuild), value);
                _autoRebuild = value;
            }
        }

        private static int _autoRebuild;
        private static bool _isPlaying;


        private void OnEnable()
        {
            _isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            _autoRebuild = AutoRebuild;
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
            RefreshAutoLoadWatcher();
        }

        private void RefreshAutoLoadWatcher()
        {
            var fullPath = Path.GetFullPath(ConfigSourcePath);
            if (!Directory.Exists(fullPath))
            {
                EditorFLibUtility.Alert($"not found config path{fullPath}");
                FLib.Log.Error?.Write($"not found config path: {fullPath}");
                return;
            }
            AutoLoadWatcher = new FileSystemWatcher(fullPath) { IncludeSubdirectories = true };
            AutoLoadWatcher.Changed += TriggerLoadCfgs;
            AutoLoadWatcher.Renamed += TriggerLoadCfgs;
            AutoLoadWatcher.EnableRaisingEvents = true;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
            AutoLoadWatcher?.Dispose();
        }

        private void CreateGUI()
        {
            var menu = new VisualElement().FlexDirection(FlexDirection.Row).FlexWrap(Wrap.Wrap);
            rootVisualElement.Add(menu);
            menu.Add(new ToolbarButton(() => BuildConfig()) { text = "编译配置表" });
            menu.Add(new ToolbarButton(() => LoadConfig()) { text = "加载配置表" });
            menu.Add(new ToolbarSpacer());
            menu.Add(new ToolbarToggle() { text = "多线程", tooltip = "开启后可以翻倍加快编译速度\n但是会导致每次编译出来的文件可能都不一样(哪怕没有修改过配置)" }.BindDataWithUI(v => IsMultithread = v, () => IsMultithread));
            menu.Add(new ToolbarToggle() { text = "自动编译加载(未运行)", tooltip = "自动监听配置文件修改\n改了配置表就自动触发编译" }.BindDataWithUI(v => AutoRebuild = v ? AutoRebuild | 1 : AutoRebuild ^ 1, () => (AutoRebuild & 1) != 0));
            menu.Add(new ToolbarToggle() { text = "自动编译加载(运行时)", tooltip = "自动监听配置文件修改\n改了配置表就自动触发编译" }.BindDataWithUI(v => AutoRebuild = v ? AutoRebuild | 2 : AutoRebuild ^ 2, () => (AutoRebuild & 2) != 0));
            menu.Add(new ToolbarToggle() { text = "启动时编译", tooltip = "在每次启动游戏的时候自动编译一次" }.BindDataWithUI(v => IsLaunchRebuild = v, () => IsLaunchRebuild));
            rootVisualElement.Add(CreateConfigSourcePath("配置表源路径", v => ConfigSourcePath = v, () => ConfigSourcePath));
        }

        /// <summary>
        /// 
        /// </summary>
        private VisualElement CreateConfigSourcePath(string label, Action<string> set, Func<string> get)
        {
            var root = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            root.Add(new TextField(label) { style = { flexGrow = 1 } }.ShortFieldLabel().BindDataWithUI(set, get));
            var bar = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            root.Add(bar);
            bar.Add(new ToolbarButton(() => Process.Start(Path.GetFullPath(get()))) { text = "打开目录" });
            bar.Add(new ToolbarButton(() => OnClickConfigPath(set)) { text = "选择目录", tooltip = "excel、json等配置表文件的根目录" });
            return root;
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnClickConfigPath(Action<string> set)
        {
            var path = EditorFLibUtility.OpenFolderPanel("config path", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                set(Path.GetRelativePath(FIO.CurrentWorkDirectory, path));
                rootVisualElement.Clear();
                RefreshAutoLoadWatcher();
                CreateGUI();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnPlayModeStateChange(PlayModeStateChange e)
        {
            _isPlaying = e is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.EnteredPlayMode;
            if (e == PlayModeStateChange.ExitingEditMode && IsLaunchRebuild)
            {
                BuildConfig(false);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void TriggerLoadCfgs(object sender, FileSystemEventArgs e)
        {
            if (!CheckIsAutoRebuild() || !File.Exists(e.FullPath)) return;
            var version = Interlocked.Increment(ref _autoLoadVersion);
            UniTask.Void(async () =>
            {
                await UniTask.SwitchToMainThread();
                if (version == _autoLoadVersion)
                {
                    BuildConfig();
                    LoadConfig();
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public static void BuildConfig(bool isLog = true)
        {
            if (isLog)
                EditorFLibUtility.ClearLog();
            if (string.IsNullOrEmpty(ConfigSourcePath))
            {
                EditorFLibUtility.Alert($"not found config path{ConfigSourcePath}");
                FLib.Log.Error?.Write($"not found config path: {ConfigSourcePath}");
                return;
            }

            var sw = Stopwatch.StartNew();
            var count = ConfigBuilder.Build(ConfigSourcePath, IsMultithread);
            if (isLog)
            {
                var fileInfo = new FileInfo(ConfigBuilder.OutputPath);
                Log($"build config[{count}] size:{FIO.FormatSize(fileInfo.Length)} {sw.ElapsedMilliseconds}ms\n{fileInfo.FullName}");
            }
            if (Application.isPlaying)
                LoadConfig();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void LoadConfig(bool isLog = true)
        {
            ConfigHelper.DeserializeAll(ConfigBuilder.OutputPath, out var count);
            if (isLog)
                Log($"load configs {count}");
        }

        /// <summary>
        /// 
        /// </summary>
        [HideInCallstack]
        public static void Log(string text)
        {
            FLib.Log.Info?.Write(text, "CONFIG");
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool CheckIsAutoRebuild() => (_isPlaying && (_autoRebuild & 2) != 0) || (!_isPlaying && (_autoRebuild & 1) != 0);
    }
}
