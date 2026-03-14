// // =================================================={By Qcbf|qcbf@qq.com|2024-2-5}==================================================
//
// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using Cysharp.Threading.Tasks;
// using UnityEditor.UIElements;
// using UnityEngine.UIElements;
//
// namespace FLib.Unity.Editor
// {
//     public class WorldEntityBehaviorEditor : TitleAreaUI
//     {
//         public WorldEntityEditor EntityEditor;
//
//         private readonly UIBindData _primary;
//         private readonly UIBindData _secondary;
//
//         public WorldEntityBehaviorEditor(WorldEntityEditor entityEditor)
//         {
//             EntityEditor = entityEditor;
//             Title = "Behavior";
//
//             var bSys = EntityEditor.Entity.GetRO<WorldBehaviorSystem>();
//             if (bSys == null)
//             {
//                 Add(new Label("not behavior system"));
//                 return;
//             }
//
//             var runningBar = new VisualElement().FlexDirection(FlexDirection.Row);
//             runningBar.style.justifyContent = Justify.SpaceAround;
//             runningBar.Add(CreateRunningBehavior(ref _primary, bSys, () => bSys.Primary));
//             runningBar.Add(CreateRunningBehavior(ref _secondary, bSys, () => bSys.Secondary));
//             Add(runningBar);
//             AddToMenuBar(new ToolbarButton(OnClickDoBehavior) { text = "执行行为" });
//
//             RegisterCallback<DetachFromPanelEvent>(OnEditorHideEvent);
//             RegisterCallback<AttachToPanelEvent>(OnEditorShowEvent);
//         }
//
//         private void OnEditorShowEvent(AttachToPanelEvent evt)
//         {
//             var bhv = EntityEditor.Entity.Behavior();
//             bhv.ListenEvent<DoBehaviorEvent>(OnDoBehaviorEvent);
//             bhv.ListenEvent<StopBehaviorEvent>(OnStopBehaviorEvent);
//         }
//
//         private void OnEditorHideEvent(DetachFromPanelEvent evt)
//         {
//             var bhv = EntityEditor.Entity.Behavior();
//             bhv.UnlistenEvent<DoBehaviorEvent>(OnDoBehaviorEvent);
//             bhv.UnlistenEvent<StopBehaviorEvent>(OnStopBehaviorEvent);
//         }
//
//         private static VisualElement CreateRunningBehavior(ref UIBindData binder, WorldBehaviorSystem behaviorSys, Func<WorldBehaviorContext> getContext)
//         {
//             var root = new VisualElement().FlexDirection(FlexDirection.Row);
//             binder = new Label().BindDataToUI(v =>
//             {
//                 var ctx = getContext();
//                 v.text = ctx == null ? "no" : CommentAttribute.TryGetLabel(ctx.Behavior.GetType());
//             }).AddToUI(root);
//             root.Add(new ToolbarButton(() =>
//                 {
//                     var ctx = getContext();
//                     ctx.BehaviorSys.Stop(ctx);
//                 })
//                 { text = "stop", focusable = false });
//             root.AddManipulator(new ContextualMenuManipulator(evt => evt.menu.AppendAction("print info", _ => Log.Info?.Write(getContext().ToString(true)))));
//             return root;
//         }
//
//
//         private void OnDoBehaviorEvent(object dispatcher, in DoBehaviorEvent e)
//         {
//             _primary.Dirty();
//             _secondary.Dirty();
//         }
//
//         private void OnStopBehaviorEvent(object dispatcher, in StopBehaviorEvent e)
//         {
//             UniTask.NextFrame().ContinueWith(() =>
//             {
//                 _primary.Dirty();
//                 _secondary.Dirty();
//             });
//         }
//
//
//         private void OnClickDoBehavior()
//         {
//             var json = DialogWindow.Open(new DialogWindow.OptionData()
//             {
//                 CustomUI = new TextField(),
//                 Btns = new[] { "cancel", "ok" },
//             }, DialogWindow.EOpenType.Modal).GetCustomUI<TextField>(1)?.value;
//             if (json == null)
//                 return;
//
//             var doBehavior = Json5.Deserialize<DoBehaviorData>(json);
//             var behavior = WorldBehaviorSystem.TypeBehaviors[TypeAssistant.GetType(doBehavior.BehaviorTypeName)];
//             var bSys = EntityEditor.Entity.Behavior();
//             if (doBehavior.Param != null)
//             {
//                 var world = EntityEditor.World;
//                 var method = bSys.GetType().GetMethod("Do", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(WorldBehavior), typeof(WorldComponentHandleEx).MakeByRefType() }, null);
//                 var typeId = WorldComponentManager.ComponentTypeIds[((ScriptPack)doBehavior.Param).UserInstance.GetType()];
//                 var handle = new WorldComponentHandleEx(world, new WorldComponentHandle(typeId, world.ComponentMgr.GetGroup(typeId).Add(EntityEditor.Entity, (IWorldComponentable)doBehavior.Param)));
//                 method!.Invoke(bSys, new object[] { behavior, handle });
//             }
//             else
//             {
//                 bSys.Do(behavior);
//             }
//         }
//
//         private struct DoBehaviorData
//         {
//             public string BehaviorTypeName;
//             public ScriptPack<object> Param;
//         }
//     }
// }
