// =================================================={By Qcbf|qcbf@qq.com|2024-07-24}==================================================

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    [CustomEditor(typeof(DefaultAsset))]
    public class FolderReadmeEditor : UnityEditor.Editor
    {
        public Dictionary<string, VisualElement> FolderUIs = new();

        public override VisualElement CreateInspectorGUI()
        {
            var root = base.CreateInspectorGUI() ?? new VisualElement();
            foreach (var obj in Selection.objects)
            {
                root.Add(RefreshFolderUI(AssetDatabase.GetAssetPath(obj)));
            }
            return root;
        }

        public VisualElement RefreshFolderUI(string folder)
        {
            if (!FolderUIs.TryGetValue(folder, out var root))
                FolderUIs.Add(folder, root = new VisualElement());
            else
                root.Clear();

            var readmeFilePath = Path.Combine(folder, ".README.md");
            if (File.Exists(readmeFilePath))
            {
                var menu = new Toolbar();
                root.Add(menu);
                menu.Add(new ToolbarButton(() =>
                {
                    File.Delete(readmeFilePath);
                    RefreshFolderUI(folder);
                }) { text = "Delete" });
                menu.Add(new ToolbarButton(() => Process.Start(readmeFilePath)) { style = { flexGrow = 10, unityTextAlign = TextAnchor.MiddleCenter }, text = "Open" });

                root.Add(new Label(MarkdownUtility.ToHtml(File.ReadAllText(readmeFilePath))) { style = { whiteSpace = WhiteSpace.Normal } });
            }
            else
            {
                root.Add(new Button(() =>
                {
                    File.WriteAllText(readmeFilePath, "# README Document");
                    RefreshFolderUI(folder);
                }) { text = "Create README" });
            }

            return root;
        }
    }
}
