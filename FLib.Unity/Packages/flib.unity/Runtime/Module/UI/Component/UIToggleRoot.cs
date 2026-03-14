// //==================={By Qcbf|qcbf@qq.com|8/10/2022 11:06:55 AM}===================
//
// using System;
// using System.Collections.Generic;
// using FLib;
// using UnityEngine;
// using UnityEngine.Events;
//
// namespace FLib.Unity
// {
//     public class UIToggleRoot : MonoBehaviour
//     {
//         public UIToggle Selected;
//         public UnityEvent<UIToggle> OnSelectEvent;
//         public UnityEvent<UIToggle> OnDeselectEvent;
//
//
// #if UNITY_EDITOR
//         [ContextMenu("自动查找")]
//         public void __AutoFind()
//         {
//             foreach (var item in transform.GetComponentsInChildren<UIToggle>(true))
//                 item.Root = this;
//             UnityEditor.EditorUtility.SetDirty(this);
//         }
// #endif
//     }
// }
