// // =================================================={By Qcbf|qcbf@qq.com|2024-2-6}==================================================
//
// using System;
// using System.Collections.Generic;
// using UnityEngine.UIElements;
//
// namespace FLib.Unity.Editor
// {
//     public class WorldComponentCustomUIAttribute : ObjectInjectToAttribute
//     {
//         public Type ComponentType;
//
//         public WorldComponentCustomUIAttribute(Type componentType)
//         {
//             Name = nameof(WorldComponentCustomUI);
//             ComponentType = componentType;
//         }
//     }
//
//     public abstract class WorldComponentCustomUI : VisualElement
//     {
//         protected WorldEntityEditor EntityEditor;
//         public WorldComponentHandle CompHandle;
//         public WorldEntity Entity => EntityEditor.Entity;
//         public WorldBase World => EntityEditor.World;
//
//         protected WorldComponentCustomUI(WorldEntityEditor entityEditor, WorldComponentHandle compHandle)
//         {
//             EntityEditor = entityEditor;
//             CompHandle = compHandle;
//         }
//     }
// }
