// // =================================================={By Qcbf|qcbf@qq.com|2024-2-5}==================================================
//
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor.UIElements;
// using UnityEngine.UIElements;
//
// namespace FLib.Unity.Editor
// {
//     public class WorldEntityComponentEditor : TitleAreaUI
//     {
//         protected readonly WorldEntityEditor _entityEditor;
//
//         private readonly Dictionary<ushort, WorldComponentEditor> mComponentUIDict = new();
//
//         public WorldEntityComponentEditor(WorldEntityEditor entityEditor)
//         {
//             _entityEditor = entityEditor;
//             Title = "Components";
//
//             var searchUI = new ToolbarSearchField().Width(120);
//             searchUI.RegisterValueChangedCallback(OnSearchChangeEvent);
//             MenuBarUI.Add(new ToolbarButton(OnClickAddNewComponent) { text = "New" });
//             MenuBarUI.Add(searchUI);
//             mComponentUIDict.EnsureCapacity(WorldComponentManager.ComponentCount);
//
//             for (ushort i = 1; i <= WorldComponentManager.ComponentCount; i++)
//             {
//                 try
//                 {
//                     var firstPos = _entityEditor.World.EntityMgr.GetEntityComponentFirstPos(_entityEditor.Entity, i);
//                     if (firstPos > 0)
//                         AddComponentUI(_entityEditor.World.EntityMgr.AllComponents[firstPos - 1].Handle);
//                 }
//                 catch (Exception ex)
//                 {
//                     Log.Error?.Write($"add component[{WorldComponentManager.GetTypeName(i)}] ui error \n{ex}");
//                 }
//             }
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         private void OnClickAddNewComponent()
//         {
//             TypeChooserWindow.Open(typeof(IWorldComponentable), null, t =>
//             {
//                 DialogWindow.Open(new DialogWindow.OptionData
//                 {
//                     Title = $"{WorldComponentManager.GetTypeName(t)} Json Data",
//                     Btns = new[] { "Cancel", "Sure" },
//                     CustomUI = DialogWindow.CreateTextArea(Json5.Serialize(TypeAssistant.New(t))),
//                     BtnClickHook = box =>
//                     {
//                         if (box.SelectBtnIndex == 1)
//                         {
//                             var json = ((TextField)box.Options.CustomUI).value;
//                             _entityEditor.Entity.Add(Json5.Deserialize<WorldComponentPack>(json));
//                         }
//                         return false;
//                     }
//                 });
//             });
//         }
//
//         /// <summary>
//         ///  
//         /// </summary>
//         public void AddComponentUI(WorldComponentHandle handle)
//         {
//             if (!mComponentUIDict.TryGetValue(handle.TypeId, out var ui))
//             {
//                 var compType = WorldComponentManager.ComponentTypes[handle.TypeId - 1];
//                 ui = (WorldComponentEditor)TypeAssistant.New(typeof(WorldComponentEditor<>).MakeGenericType(compType), _entityEditor, handle);
//                 Add(WorldEntityEditor.PrefsBool(ui, ui.name));
//                 mComponentUIDict.Add(handle.TypeId, ui);
//                 ui.RefreshComponentLife();
//             }
//             else
//             {
//                 ui.RefreshComponentLife();
//             }
//         }
//
//         /// <summary>
//         ///  
//         /// </summary>
//         public void RemoveComponent(WorldComponentHandle handle)
//         {
//             if (mComponentUIDict.TryGetValue(handle.TypeId, out var ui))
//             {
//                 if (_entityEditor.Entity.Exist(ui.Component.TypeId) && _entityEditor.Entity.GetAll(ui.Component.TypeId).Count() > 1)
//                 {
//                     ui.RefreshComponentLife();
//                 }
//                 else
//                 {
//                     mComponentUIDict.Remove(handle.TypeId);
//                     Remove(ui);
//                 }
//             }
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         private void OnSearchChangeEvent(ChangeEvent<string> evt)
//         {
//             foreach (var item in mComponentUIDict)
//             {
//                 item.Value.style.display = item.Value.name.Contains(evt.newValue, StringComparison.OrdinalIgnoreCase) ? DisplayStyle.Flex : DisplayStyle.None;
//             }
//         }
//     }
// }
