//==================={By Qcbf|qcbf@qq.com|7/16/2021 11:45:40 AM}===================

#pragma warning disable IDE1006 // Naming Styles
using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public abstract class BaseEditor<T> : UnityEditor.Editor where T : Object
    {
        public new T target => base.target as T;
        public virtual bool SupportUndo => true;
        public VisualElement RootUI;

        protected virtual void OnEnable()
        {
            if (SupportUndo)
                Undo.undoRedoPerformed += OnDoRedo;
        }

        protected virtual void OnDisable()
        {
            if (SupportUndo)
                Undo.undoRedoPerformed -= OnDoRedo;
        }

        private void OnDoRedo()
        {
            RefreshUI();
        }

        public override VisualElement CreateInspectorGUI()
        {
            RootUI = new VisualElement { style = { flexGrow = 1 } };
            RefreshUI();
            return RootUI;
        }

        public virtual void RefreshUI()
        {
            RootUI?.Clear();
            foreach (var item in targets)
            {
                if (item is T val)
                    CreateUI(val);
            }
        }

        public virtual void SetState(Action<T> act, bool? isSupportUndo = null, bool isRefreshUI = true)
        {
            foreach (var item in targets)
            {
                if (item is not T val)
                    continue;
                if (isSupportUndo != null)
                {
                    if (isSupportUndo == true)
                        Undo.RecordObject(item, string.Empty);
                }
                else if (SupportUndo)
                    Undo.RecordObject(item, string.Empty);
                act(val);
                EditorUtility.SetDirty(item);
                AssetDatabase.SaveAssetIfDirty(item);
            }
            if (isRefreshUI)
                RefreshUI();
        }

        public abstract void CreateUI(T targetObject);
    }
}
