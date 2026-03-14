// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.PackBuilder
{
    public class ShaderVariantTool : EditorWindow
    {
        public Func<int> GetCurrentShaderVariantCollectionShaderCount => CreateMethod<Func<int>>("GetCurrentShaderVariantCollectionShaderCount");
        public Func<int> GetCurrentShaderVariantCollectionVariantCount => CreateMethod<Func<int>>("GetCurrentShaderVariantCollectionVariantCount");
        public Action ClearCurrentShaderVariantCollection => CreateMethod<Action>("ClearCurrentShaderVariantCollection");
        public Action<string> SaveCurrentShaderVariantCollection => CreateMethod<Action<string>>("SaveCurrentShaderVariantCollection");


        private ShaderVariantCollection _shaderVariantCollection;
        private UIBindGroup _bindGroup;

        private void Awake()
        {
            titleContent.text = "Shader Variant Tool";
            var guid = AssetDatabase.FindAssets("t:ShaderVariantCollection", new[] { Utility.GameResFolder }).FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
                _shaderVariantCollection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void CreateGUI()
        {
            var bar = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            rootVisualElement.Add(bar);
            new ObjectField() { name = nameof(_shaderVariantCollection), objectType = typeof(ShaderVariantCollection), allowSceneObjects = false, style = { flexGrow = 1 } }
                .BindDataWithUI(v => _shaderVariantCollection = (ShaderVariantCollection)v, () => _shaderVariantCollection)
                .AddToUI(bar);
            new Label().TextAlign(TextAnchor.MiddleLeft)
                .BindDataToUI(ui => ui.text = $"{_shaderVariantCollection.shaderCount}/{_shaderVariantCollection.variantCount}")
                .AddGroup(ref _bindGroup)
                .AddToUI(bar);

            bar.Add(new ToolbarSpacer());
            var currentInfoLabel = new Label().TextAlign(TextAnchor.MiddleLeft);
            currentInfoLabel.schedule.Execute(() => currentInfoLabel.text = $"Collecting {GetCurrentShaderVariantCollectionShaderCount()}/{GetCurrentShaderVariantCollectionVariantCount()}").Every(100);
            bar.Add(currentInfoLabel);
            bar.Add(new ToolbarButton(ClearCurrentShaderVariantCollection) { text = "clear" });

            rootVisualElement.Add(new Button(CreateCollectionAsset) { text = "write collection variants to asset" });
        }

        private void CreateCollectionAsset()
        {
            const string tempPath = "assets/shaderVariantCollectionTemp.asset";
            SaveCurrentShaderVariantCollection(tempPath);
            var tempShaderVariantCollection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(tempPath);
            var log = new StringBuilder();
            try
            {
                using var serializedSource = new SerializedObject(tempShaderVariantCollection);
                using var variants = serializedSource.FindProperty("m_Shaders.Array");
                for (var i = 0; i < variants.arraySize; i++)
                {
                    var shaderData = variants.GetArrayElementAtIndex(i);
                    var shader = (Shader)shaderData.FindPropertyRelative("first").objectReferenceValue;
                    var variantArray = shaderData.FindPropertyRelative("second.variants");
                    for (var j = 0; j < variantArray.arraySize; j++)
                    {
                        var variant = variantArray.GetArrayElementAtIndex(j);
                        var keywords = variant.FindPropertyRelative("keywords").stringValue.Split(' ');
                        var passType = (PassType)variant.FindPropertyRelative("passType").intValue;
                        _shaderVariantCollection.Add(new ShaderVariantCollection.ShaderVariant(shader, passType, keywords));
                    }
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(tempPath);
            }
            AssetDatabase.SaveAssets();
            Log.Info?.Write(log.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        private static T CreateMethod<T>(string name) where T : Delegate
            => (T)Delegate.CreateDelegate(typeof(T), typeof(ShaderUtil).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);
    }
}
