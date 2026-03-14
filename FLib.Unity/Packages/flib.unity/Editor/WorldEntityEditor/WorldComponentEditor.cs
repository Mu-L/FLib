// // =================================================={By Qcbf|qcbf@qq.com|2024-2-5}==================================================
//
// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Text;
// using UnityEditor;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace FLib.Unity.Editor
// {
//     [ObjectInjectionReceiver(nameof(WorldComponentCustomUI), nameof(ReceiveInjection))]
//     public abstract class WorldComponentEditor : TitleAreaUI
//     {
//         public static Dictionary<Type, Type> CustomUIs;
//         public WorldComponentHandle Component;
//         protected WorldEntityEditor _entityEditor;
//         protected UIBindData _titleBinder;
//
//         public static void ReceiveInjection(List<(object type, ObjectInjectToAttribute attr)> list)
//         {
//             CustomUIs = new(list.Count);
//             foreach (var (type, attr) in list)
//                 CustomUIs.Add(((WorldComponentCustomUIAttribute)attr).ComponentType, (Type)type);
//         }
//
//         protected WorldComponentEditor(WorldEntityEditor entityEditor, WorldComponentHandle compHandle)
//         {
//             Component = compHandle;
//             name = WorldComponentManager.GetTypeName(compHandle.TypeId);
//             _entityEditor = entityEditor;
//             style.paddingTop = style.paddingBottom = 2;
//             MenuBarUI.AddManipulator(new ContextualMenuManipulator(InitContextMenu));
//             _titleBinder = TitleUI.BindDataToUI(v =>
//             {
//                 var count = _entityEditor.Entity.GetAll(Component.TypeId).Count();
//                 v.text = count > 1 ? $"{name}*{count}" : name;
//             });
//             AddContentUI();
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         protected virtual void InitContextMenu(ContextualMenuPopulateEvent e)
//         {
//             e.menu.AppendAction(name, _ => OnClickOpen());
//             e.menu.AppendAction(string.Empty, null);
//             e.menu.AppendAction("Remove", _ => OnClickRemove());
//             e.menu.AppendAction(nameof(PrintInfoText), _ => PrintInfoText());
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public virtual void AddContentUI()
//         {
//             if (CustomUIs.TryGetValue(WorldComponentManager.ComponentTypes[Component.TypeId - 1], out var contentUIType))
//             {
//                 Add((WorldComponentCustomUI)TypeAssistant.New(contentUIType, _entityEditor, Component));
//             }
//             else
//             {
//                 ToggleUI.visible = false;
//             }
//         }
//
//         /// <summary>
//         /// 
//         /// </summary>
//         public virtual void RefreshComponentLife()
//         {
//             _titleBinder.Dirty();
//         }
//
//         protected abstract void OnClickOpen();
//         protected abstract void OnClickRemove();
//         protected abstract void PrintInfoText();
//     }
//
//     /// <summary>
//     /// 
//     /// </summary>
//     public class WorldComponentEditor<T> : WorldComponentEditor where T : IWorldComponentable, new()
//     {
//         public WorldComponentEditor(WorldEntityEditor entityEditor, WorldComponentHandle compHandle) : base(entityEditor, compHandle)
//         {
//         }
//
//         protected override void OnClickOpen()
//         {
//             var filePath = Directory.GetFiles("./", $"{typeof(T).Name}.cs", SearchOption.AllDirectories).SingleOrDefault() ??
//                            Directory.GetFiles("../", $"{typeof(T).Name}.cs", SearchOption.AllDirectories).SingleOrDefault();
//             Log.AssertNotNull(filePath)?.Write($"not found {typeof(T).Name}.cs");
//             using var proc = Process.Start(Path.GetFullPath(filePath));
//         }
//
//         protected override void OnClickRemove()
//         {
//             var comps = _entityEditor.Entity.GetAll<T>();
//             if (comps.MoveNext())
//             {
//                 if (comps.MoveNext())
//                 {
//                     DialogWindow.Open(new DialogWindow.OptionData
//                     {
//                         Title = $"Delete {Title}",
//                         Btns = new[] { "Cancel", nameof(PrintInfoText), "Sure" },
//                         CustomUI = new IntegerField("Component Index").ShortFieldLabel(),
//                         BtnClickHook = box =>
//                         {
//                             if (box.SelectBtnIndex == 2)
//                             {
//                                 var index = (byte)box.GetCustomUI<IntegerField>().value;
//                                 var comp = comps.ElementAt(index);
//                                 _entityEditor.Entity.Remove(comp);
//                             }
//                             else if (box.SelectBtnIndex == 1)
//                             {
//                                 PrintInfoText();
//                                 return true;
//                             }
//                             return false;
//                         }
//                     });
//                 }
//                 else
//                 {
//                     _entityEditor.Entity.Remove(comps.ElementAt(0));
//                 }
//             }
//             else
//             {
//                 if (EditorFLibUtility.AlertSure($"Delete {Title} ?"))
//                     _entityEditor.Entity.Remove(Component);
//             }
//         }
//
//         protected override void PrintInfoText()
//         {
//             var strbuf = StringFLibUtility.GetStrBuf().Append(Title).AppendLine();
//             var t = typeof(T);
//             foreach (var item in _entityEditor.Entity.GetAll<T>())
//                 strbuf.AppendLine(item.ToString(true));
//             Log.Info?.Write(StringFLibUtility.ReleaseStrBufAndResult(strbuf));
//         }
//     }
// }
