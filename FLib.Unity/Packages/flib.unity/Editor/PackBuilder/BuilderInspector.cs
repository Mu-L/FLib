//==================={By Qcbf|qcbf@qq.com|10/20/2021 3:31:43 PM}===================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor.PackBuilder
{
    [InitializeOnLoad]
    public static class BuilderInspector
    {
        private static readonly List<(AssetImporter, Object)> Targets = new();

        static BuilderInspector()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        private static void OnPostHeaderGUI(UnityEditor.Editor obj)
        {
            foreach (var target in obj.targets)
            {
                var path = AssetDatabase.GetAssetPath(target);
                if (path.Length == 0 || !path.StartsWith(Utility.GameResFolder) || !CheckType(target.GetType(), path))
                    return;
            }

            Targets.Clear();
            var loadableCount = 0;

            foreach (var item in obj.targets)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item));
                Targets.Add((importer, item));
                if (importer.IsLoadable())
                {
                    loadableCount++;
                }
            }

            if (Targets.Count == loadableCount)
            {
                GUILayout.BeginHorizontal();
                var isCanceled = false;
                GUI.color = Color.green;
                if (!GUILayout.Toggle(true, Utility.GUILoadableLabel, Utility.GUILoadableLabelLayout))
                {
                    isCanceled = true;
                    foreach (var item in Targets)
                        item.Item1.SetLoadable(false);
                }
                GUI.color = Color.white;
                if (!isCanceled && Targets[0].Item2 is DefaultAsset)
                {
                    var filePath = Path.Combine(Targets[0].Item1.assetPath, Utility.LoadableFolderFileName);
                    var content = File.ReadAllText(filePath);
                    EditorGUI.BeginChangeCheck();
                    content = EditorGUILayout.DelayedTextField(content);
                    if (EditorGUI.EndChangeCheck())
                        File.WriteAllText(filePath, content);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Toggle(false, Utility.GUILoadableLabel))
                {
                    foreach (var item in Targets)
                        item.Item1.SetLoadable(true);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        private static bool CheckType(Type t, string path)
        {
            if (t == typeof(DefaultAsset))
                return Directory.Exists(path);
            return t == typeof(GameObject) || t == typeof(TextureImporter) || t == typeof(AudioImporter) || t == typeof(SpriteAtlasImporter) ||
                   typeof(ScriptableObject).IsAssignableFrom(t) || t == typeof(AnimatorController) || t == typeof(TextAsset) || t == typeof(Material)
                   || t == typeof(SceneAsset) || path.EndsWith(".asset");
        }
    }
}
