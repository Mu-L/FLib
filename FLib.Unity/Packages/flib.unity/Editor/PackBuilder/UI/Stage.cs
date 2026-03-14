// ==================== qcbf@qq.com | 2025-07-01 ====================

using System.IO;
using System.Text;
using FLib.Unity.Editor.PackBuilder.Task;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.PackBuilder.UI
{
    public class Stage : EditorWindow
    {
        public const string Name = "打包工具";

        private PlayerPrefsStringField _json = new($"{nameof(PackBuilder)}Json", @"
{
	IsDevelop: 1,
	IsDeepDevelop: 0,
	Tasks: [
		{$:AssetBundle},
		// {$:PatchAssetBundle, IsOpenOutputDir: 1},
		{$:CopyAsset},
		{$:Publish, IsOpenOutputDir:1, IsZip:0, Version: 0.0.1, IsIncrementVersion: 0},
	]
}
");

        private void CreateGUI()
        {
            CreateMenu();
            new TextField() { multiline = true, style = { flexGrow = 1 } }.BindDataWithUI(v => _json.Set(v), () => _json.Get()).AddToUI(rootVisualElement);
        }

        private void CreateMenu()
        {
            var bar = new VisualElement().FlexDirection(FlexDirection.Row).FlexWrap(Wrap.Wrap);
            rootVisualElement.Add(bar);
            bar.Add(new ToolbarToggle
            {
                text = "资源包模式",
#if ASSET_BUNDLE
                style = { color = Color.red, unityFontStyleAndWeight = FontStyle.Bold}
#endif
            }.BindDataWithUI(v => Utility.IsAssetBundleMode = v, () => Utility.IsAssetBundleMode));
            bar.Add(new ToolbarButton(AssetBundleBrowser.AssetBundleBrowserMain.ShowWindow) { text = "Bundle预览" });
            bar.Add(new ToolbarButton(() => GetWindow<ShaderVariantTool>()) { text = "Shader Variant" });
            bar.Add(new ToolbarButton(() => GetWindow<BuilderAdbTool>()) { text = "ADB Tool" });
            bar.Add(new ToolbarButton(OnClickPrintInfo) { text = "打印Info文件" });

            bar.Add(new VisualElement().FlexGrow(1));

            bar.Add(new ToolbarButton(OnClickBuildPack) { text = "打包", style = { unityFontStyleAndWeight = FontStyle.Bold } });
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnClickPrintInfo()
        {
            var infoPath = EditorUtility.OpenFilePanel("", Path.GetFullPath(Utility.InfoPath), "");
            if (string.IsNullOrEmpty(infoPath)) return;
            var str = AssetLoaderInfo.Unpack(File.ReadAllBytes(infoPath)).GetLog();
            EditorFLibUtility.ClipboardTxt = str;
            Log.Info?.Write(str, Name);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnClickBuildPack()
        {
            if (EditorFLibUtility.AlertSure("确定打包"))
                TaskSchedule.Do(_json.Get());
        }

        // /// <summary>
        // /// 
        // /// </summary>
        // private void OnClickGenerateBat()
        // {
        //     var strbuf = new StringBuilder();
        //     strbuf.AppendLine("@echo off");
        //     strbuf.AppendLine("svn update --accept theirs-full ./");
        //
        //     strbuf.Append($"\"{EditorApplication.applicationPath}\" -projectPath \"{Path.GetFullPath(".")}\" -batchMode -quit -executeMethod {typeof(CommandLine).FullName}.Do -logFile unity-build-pack.log --json \"");
        //     foreach (var jsonLine in Json.Get().Split('\n'))
        //     {
        //         var s = jsonLine.Trim();
        //         if (!s.StartsWith("//"))
        //             strbuf.Append(s);
        //     }
        //     strbuf.AppendLine("\"");
        //     strbuf.AppendLine("echo completed!!");
        //     strbuf.AppendLine("pause");
        //
        //     var str = strbuf.ToString();
        //     EditorFLibUtility.ClipboardTxt = str;
        //     str = "已经复制\n" + str;
        //     EditorFLibUtility.Alert(str);
        //     Log.Info?.Write(str);
        // }
    }
}
