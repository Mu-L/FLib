// using System;
// using System.Collections.Generic;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.UI;
// using Object = UnityEngine.Object;
//
// namespace FLib.Unity
// {
//     public class UIInitializer : ScriptableObject
//     {
//         public Option Options;
//         public static RootData PrimaryRoot;
//         public static RootData SecondaryRoot;
//         public static bool IsPrimaryRoot = true;
//         public static string SecondaryPrefabPrefix;
//
//         public static RootData Root => IsPrimaryRoot ? PrimaryRoot : SecondaryRoot;
//         public static Canvas Canvas => Root.Canvas;
//         public static Camera UICamera => Root.UICamera;
//         public static UIContainer[] Layers => Root.Layers;
//         public static Vector2 Resolution => Root.Resolution;
//
//         public class RootData
//         {
//             public Canvas Canvas;
//             public Camera UICamera;
//             public UIContainer[] Layers;
//             public Vector2 Resolution;
//         }
//
//         [Serializable]
//         public class Option
//         {
//             public string SecondaryPrefabSuffix;
//             public int LayerOrderInterval = 1000;
//             public LayerDefine[] Layers = { "Background", "Window", "Popup" };
//         }
//
//         [Serializable]
//         public struct LayerDefine
//         {
//             public string Name;
//             public bool EnableHistory;
//             public Rect? SafeArea;
//             public override string ToString() => Name;
//             public static implicit operator LayerDefine(string name) => new() { Name = name };
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public static async UniTask Initialize(string path)
//         {
//             var loaded = await AssetLoader.Load(path);
//             var options = loaded.GetMainAsset<UIInitializer>().Options;
//             loaded.IsUnloadAll = false;
//             loaded.Unload();
//             Initialize(options);
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public static void Initialize(Option option)
//         {
//             SecondaryPrefabPrefix = option.SecondaryPrefabSuffix;
//             var rootCanvas = FindObjectsByType(typeof(CanvasScaler), FindObjectsInactive.Include, FindObjectsSortMode.None);
//             if (rootCanvas.Length == 0)
//                 throw new Exception("not found ui root");
//             var uiUnityLayer = LayerMask.NameToLayer("UI");
//             for (var i = 0; i < rootCanvas.Length; i++)
//             {
//                 var rootScaler = (CanvasScaler)rootCanvas[i];
//                 var root = new RootData() { Canvas = rootScaler.GetComponent<Canvas>() };
//                 if (rootScaler.isActiveAndEnabled)
//                     PrimaryRoot = root;
//                 else
//                     SecondaryRoot = root;
//                 root.UICamera = root.Canvas.worldCamera;
//                 root.Resolution = rootScaler.referenceResolution;
//                 root.Layers = new UIContainer[option.Layers.Length];
//                 var startLayerIndex = 0;
//                 for (var j = 0; j < option.Layers.Length; j++)
//                 {
//                     var layerDef = option.Layers[j];
//                     var layerGo = new GameObject(layerDef.Name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)) { layer = uiUnityLayer };
//                     var rtf = layerGo.GetComponent<RectTransform>();
//                     var layer = root.Layers[j] = new UIContainer() { Root = rtf };
//                     rtf.SetParent(rootScaler.transform, false);
//                     rtf.SetSiblingIndex(j);
//                     rtf.anchorMin = Vector2.zero;
//                     rtf.anchorMax = Vector2.one;
//                     rtf.localPosition = Vector2.zero;
//                     rtf.sizeDelta = Vector2.zero;
//                     var canvas = layerGo.GetComponent<Canvas>();
//                     canvas.overrideSorting = true;
//                     canvas.vertexColorAlwaysGammaSpace = true;
//                     canvas.sortingOrder = startLayerIndex += option.LayerOrderInterval;
//                     if (layerDef.SafeArea != null)
//                         SetSafeArea(rtf, layerDef.SafeArea.Value);
//                     if (layerDef.EnableHistory)
//                         layer.History = new List<UIContext>(8);
//                 }
//             }
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public static void SetSafeArea(RectTransform rtf, Rect area)
//         {
//             var anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
//             var anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
//             rtf.anchorMin = anchorMin;
//             rtf.anchorMax = anchorMax;
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public static void SwitchRoot()
//         {
//             var curRoot = Root;
//             curRoot.Canvas.gameObject.SetActive(false);
//
//             IsPrimaryRoot = !IsPrimaryRoot;
//             var newRoot = Root;
//             for (var i = 0; i < curRoot.Layers.Length; i++)
//                 curRoot.Layers[i].SwitchTo(newRoot.Layers[i]);
//             newRoot.Canvas.gameObject.SetActive(true);
//         }
//     }
// }
