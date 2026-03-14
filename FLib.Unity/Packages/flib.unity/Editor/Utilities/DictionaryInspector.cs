// ==================== qcbf@qq.com | 2025-07-01 ====================

using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    [CustomPropertyDrawer(typeof(IDictionary), true)]
    public class DictionaryInspector : PropertyDrawer
    {
        public class Drawer : TitleAreaUI
        {
            private readonly Object _target;
            private readonly IDictionary _dict;
            private readonly AnyObjectField _keyUI;
            private readonly AnyObjectField _valueUI;
            private readonly UIBindData _countLabelBind;

            public Drawer(IDictionary dict, Object target, string autoFoldoutKey = null) : base(autoFoldoutKey)
            {
                _dict = dict;
                _target = target;
                MenuBarUI.FlexGrow(0);
                var kvType = _dict.GetType().GetGenericArguments();
                _countLabelBind = new Label($"[{_dict.Count}]").BindDataToUI(ui => ui.text = $"[{_dict.Count}]").AddToUI(MenuBarUI);
                AddToMenuBar(_keyUI = new AnyObjectField(string.Empty, kvType[0]) { style = { flexGrow = 1 } });
                AddToMenuBar(_valueUI = new AnyObjectField(string.Empty, kvType[1]) { style = { flexGrow = 2 } });
                AddToMenuBar(new Button(OnClickAdd) { text = "+" });

                RefreshItems();
            }

            /// <summary>
            /// 
            /// </summary>
            private void RefreshItems()
            {
                Clear();
                _countLabelBind.Dirty();
                var kvType = _dict.GetType().GetGenericArguments();
                var count = 0;
                foreach (DictionaryEntry item in _dict)
                {
                    var key = item.Key;
                    var val = item.Value;
                    var bar = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
                    Add(bar);
                    new AnyObjectField(string.Empty, kvType[0]) { style = { flexGrow = 1 } }.BindDataWithUIVerify(v =>
                    {
                        if (_dict.Contains(v))
                        {
                            Log.Error?.Write($"exist key: {v}");
                            return false;
                        }
                        _dict.Remove(key);
                        _dict.Add(key = v, val);
                        EditorUtility.SetDirty(_target);
                        return false;
                    }, () => key).AddToUI(bar);
                    new AnyObjectField(string.Empty, kvType[1]) { style = { flexGrow = 2 } }.BindDataWithUI(v =>
                    {
                        _dict[key] = v;
                        EditorUtility.SetDirty(_target);
                    }, () => val).AddToUI(bar);
                    bar.Add(new Button(() =>
                    {
                        if (EditorFLibUtility.AlertSure($"remove {key} ?"))
                        {
                            _dict.Remove(key);
                            EditorUtility.SetDirty(_target);
                            RefreshItems();
                        }
                    }) { text = "x" });
                    if (count++ > 4096)
                        break;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            private void OnClickAdd()
            {
                if (_keyUI.value == null || _dict.Contains(_keyUI.value))
                {
                    Log.Error?.Write($"key error: {_keyUI.value}");
                    return;
                }
                _dict.Add(_keyUI.value, _valueUI.value);
                EditorUtility.SetDirty(_target);
                RefreshItems();
                _keyUI.value = null;
                _valueUI.value = null;
            }
        }


        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var field = target.GetType().GetField(property.name);
            var dict = (IDictionary)field.GetValue(target);
            return new Drawer(dict, target, field.Name);
        }
    }
}
