// // =================================================={By Qcbf|qcbf@qq.com|2024-2-5}==================================================
//
// using System;
// using Cysharp.Threading.Tasks;
// using UnityEditor;
// using UnityEngine.UIElements;
//
// namespace FLib.Unity.Editor
// {
//     [CustomEditor(typeof(WorldEntityEditorHelper), true), CanEditMultipleObjects]
//     public class WorldEntityEditor : BaseEditor<WorldEntityEditorHelper>
//     {
//         private WorldEntityComponentEditor mComponentsUI;
//
//         public WorldEntity Entity => target.Entity;
//         public WorldBase World => target.Entity.World;
//
//         public override void CreateUI(WorldEntityEditorHelper targetObject)
//         {
//             RootUI.Add(new Label(Entity.Id.ToString()));
//             if (Entity.Exist<WorldBehaviorSystem>())
//                 RootUI.Add(PrefsBool(new WorldEntityBehaviorEditor(this), nameof(WorldBehaviorSystem), true));
//             RootUI.Add(PrefsBool(mComponentsUI = new WorldEntityComponentEditor(this), nameof(WorldComponentManager), true));
//         }
//
//         protected override void OnEnable()
//         {
//             base.OnEnable();
//             if (Entity.IsEmpty)
//                 UniTask.DelayFrame(1).ContinueWith(Listen).Forget();
//             else
//                 Listen();
//             return;
//
//             void Listen()
//             {
//                 if (World == null) return;
//                 World.ListenEvent<WorldAddComponentEvent>(OnAddComponentEvent);
//                 World.ListenEvent<WorldRemoveComponentEvent>(OnRemoveComponentEvent);
//                 World.ListenEvent<WorldEntityLifeEvent>(OnEntityEvent);
//             }
//         }
//
//         protected override void OnDisable()
//         {
//             base.OnDisable();
//             if (Entity.IsEmpty || World == null) return;
//             World.UnlistenEvent<WorldAddComponentEvent>(OnAddComponentEvent);
//             World.UnlistenEvent<WorldRemoveComponentEvent>(OnRemoveComponentEvent);
//             World.ListenEvent<WorldEntityLifeEvent>(OnEntityEvent);
//         }
//
//         private void OnEntityEvent(object dispatcher, in WorldEntityLifeEvent value)
//         {
//             if (RootUI == null || !value.IsDestroying || value.Entity != Entity) return;
//             RootUI.Add(new Label(value.Entity.ToString(true)));
//             RootUI.Clear();
//         }
//
//         private void OnAddComponentEvent(object dispatcher, in WorldAddComponentEvent e)
//         {
//             if (e.Entity != target.Entity)
//                 return;
//             mComponentsUI?.AddComponentUI(e.CompHandle);
//         }
//
//         private void OnRemoveComponentEvent(object dispatcher, in WorldRemoveComponentEvent e)
//         {
//             if (e.Entity != target.Entity)
//                 return;
//             mComponentsUI?.RemoveComponent(e.CompHandle);
//         }
//
//         public static UIBindData PrefsBool<T>(in T uiElement, string name, bool defaultState = false)
//             where T : VisualElement, INotifyValueChanged<bool>
//         {
//             return uiElement.BindDataWithUI(v => EditorPrefs.SetBool($"{nameof(WorldEntityEditor)}.{name}", v),
//                 () => EditorPrefs.GetBool($"{nameof(WorldEntityEditor)}.{name}", defaultState));
//         }
//     }
// }