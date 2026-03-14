// //==================={By Qcbf|qcbf@qq.com|9/16/2021 5:34:01 PM}===================
//
// using FLib;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using UnityEditor;
// using UnityEditor.SceneManagement;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace FLib.Unity.Editor
// {
//     public class GameResPathField : VisualElement, INotifyValueChanged<string>
//     {
//
//         public string Extension;
//
//         private string mValue = string.Empty;
//         private readonly Button mPathButton;
//         private Label mLabelUI;
//
//
//         public string value
//         {
//             get => mValue;
//             set
//             {
//                 if (value != mValue)
//                 {
//                     using var e = ChangeEvent<string>.GetPooled();
//                     e.target = this;
//                     SetValueWithoutNotify(value);
//                     SendEvent(e);
//                 }
//             }
//         }
//
//         /// <summary>
//         ///
//         /// </summary>
//         public GameResPathField(string directory = "GameRes", string extension = "prefab")
//         {
//             Extension = extension;
//             style.flexDirection = FlexDirection.Row;
//             style.flexGrow = 1;
//             style.height = 18;
//
//             Add(mPathButton = new ToolbarButton(() =>
//             {
//                 var path = EditorUtility.OpenFilePanel(mLabelUI?.text, directory, Extension);
//                 if (!string.IsNullOrEmpty(path))
//                 {
//                     value = EditorFLibUtility.TrimToGameResPath(path);
//                 }
//             }));
//             mPathButton.style.flexGrow = 1;
//             mPathButton.style.textOverflow = TextOverflow.Ellipsis;
//             mPathButton.style.unityTextOverflowPosition = TextOverflowPosition.Middle;
//             mPathButton.style.overflow = Overflow.Hidden;
//             mPathButton.style.whiteSpace = WhiteSpace.NoWrap;
//
//             Add(new ToolbarButton(() => value = string.Empty) { text = "×" });
//
//         }
//
//         /// <summary>
//         ///
//         /// </summary>
//         public GameResPathField SetLabel(string v)
//         {
//             Insert(0, mLabelUI ??= new Label());
//             mLabelUI.text = v;
//             return this;
//         }
//
//         /// <summary>
//         ///
//         /// </summary>
//         public GameResPathField SetInstantiateButton(Action<GameObject> onInstantiateClick = null, string label = "实列化")
//         {
//             var btn = this.Q<ToolbarButton>(nameof(SetInstantiateButton));
//             if (btn == null)
//             {
//                 btn = new ToolbarButton(() =>
//                 {
//                     var unityPath = EditorFLibUtility.GameResPathToUnityFullPath(mValue) + "." + Extension;
//                     var go = (GameObject)AssetDatabase.LoadMainAssetAtPath(unityPath);
//                     if (go == null) throw new Exception("找不到路径:" + unityPath);
//                     go = (GameObject)PrefabUtility.InstantiatePrefab(go, PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot?.transform);
//                     go.transform.SetAsFirstSibling();
//                     EditorUtility.SetDirty(go);
//                     onInstantiateClick?.Invoke(go);
//                 })
//                 { name = nameof(SetInstantiateButton) };
//                 Add(btn);
//             }
//             btn.text = label;
//             return this;
//         }
//
//         /// <summary>
//         ///
//         /// </summary>
//         public GameResPathField SetPlaySound(string label = "播放")
//         {
//             var btn = this.Q<ToolbarButton>(nameof(SetPlaySound));
//             if (btn == null)
//             {
//                 btn = new ToolbarButton(() =>
//                 {
//                     Log.Error?.Write("TODO");
//                 })
//                 { name = nameof(SetPlaySound) };
//                 Add(btn);
//             }
//             btn.text = label;
//             return this;
//         }
//
//         /// <summary>
//         ///
//         /// </summary>
//         public GameResPathField SetOpenButton(Action onOpenClick = null, string label = "打开")
//         {
//             var btn = this.Q<ToolbarButton>(nameof(SetOpenButton));
//             if (btn == null)
//             {
//                 btn = new ToolbarButton(() =>
//                 {
//                     var unityPath = EditorFLibUtility.GameResPathToUnityFullPath(mValue) + "." + Extension;
//                     AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(unityPath));
//                     onOpenClick?.Invoke();
//                 })
//                 { name = nameof(SetOpenButton) };
//                 Add(btn);
//             }
//             btn.text = label;
//             return this;
//         }
//
//
//         /// <summary>
//         ///
//         /// </summary>
//         public void SetValueWithoutNotify(string newValue)
//         {
//             mValue = newValue;
//             mPathButton.text = string.IsNullOrEmpty(mValue) ? "未选择" : Path.GetFileNameWithoutExtension(mValue);
//         }
//
//     }
// }
