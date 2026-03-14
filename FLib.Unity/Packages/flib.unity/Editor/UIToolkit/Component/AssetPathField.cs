//==================={By Qcbf|qcbf@qq.com|12/1/2021 10:28:46 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class AssetPathField : VisualElement, INotifyValueChanged<string>
    {
        public readonly ObjectField PreviewField;

        private string mPath;

        public string value
        {
            get => mPath;
            set
            {
                if (mPath == value)
                    return;
                using var e = ChangeEvent<string>.GetPooled(mPath, value);
                e.target = this;
                SetValueWithoutNotify(value);
                SendEvent(e);
            }
        }

        public AssetPathField(Type assetType, string name = null)
        {
            style.flexDirection = FlexDirection.Row;
            style.flexGrow = 1;
            if (name != null)
                Add(new Label(name) { style = { unityTextAlign = TextAnchor.MiddleRight } });
            Add(PreviewField = new() { allowSceneObjects = false, objectType = assetType, style = { flexGrow = 1 } });
            PreviewField.RegisterValueChangedCallback(OnPreviewValueChange);
        }

        private void OnPreviewValueChange(ChangeEvent<Object> evt)
        {
            value = evt.newValue == null ? null : AssetDatabase.GetAssetPath(evt.newValue);
        }

        public void SetValueWithoutNotify(string newValue)
        {
            mPath = newValue;
            PreviewField.SetValueWithoutNotify(AssetDatabase.LoadMainAssetAtPath(value));
            PreviewField.tooltip = value;
        }
    }
}
