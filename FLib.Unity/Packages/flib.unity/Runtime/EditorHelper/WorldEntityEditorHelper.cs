// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Diagnostics;
using FLib.WorldCores.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    public class WorldEntityEditorHelper : MonoBehaviour
    {
        public WorldEntityHelper Entity;

        [Conditional("UNITY_EDITOR")]
        public static void Set(Object target, WorldEntityHelper entity)
        {
            if (target == null)
                return;
            if (target is GameObject go)
                (go.GetComponent<WorldEntityEditorHelper>() ?? go.AddComponent<WorldEntityEditorHelper>()).Entity = entity;
            else if (target is Component comp)
                (comp.gameObject.GetComponent<WorldEntityEditorHelper>() ?? comp.gameObject.AddComponent<WorldEntityEditorHelper>()).Entity = entity;
            else
                throw new NotSupportedException(target != null ? target.ToString() : null);
        }

#if UNITY_EDITOR
        private void Awake()
        {
            while (!Application.isPlaying && UnityEditorInternal.ComponentUtility.MoveComponentUp(this))
            {
            }
        }
#endif

        [ContextMenu(nameof(DestroySelf))]
        internal void DestroySelf()
        {
            Entity.World.RemoveEntity(Entity.Entity);
        }
    }
}
