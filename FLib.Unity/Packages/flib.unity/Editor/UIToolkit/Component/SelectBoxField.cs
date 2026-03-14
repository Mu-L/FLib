//==================={By Qcbf|qcbf@qq.com|7/22/2021 2:19:58 PM}===================

using FLib;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace FLib.Unity.Editor
{
    /// <summary>
    /// 下拉选择框
    /// </summary>
    public class SelectBoxField : VisualElement, INotifyValueChanged<int>
    {
        public IEnumerable<ItemData> Items;
        public Button Button;
        public string Label;
        public bool IsSingleFlag;

        private int mValue;

        public int value
        {
            get => mValue;
            set
            {
                using var changeEvent = ChangeEvent<int>.GetPooled(mValue, value);
                changeEvent.target = this;
                SetValueWithoutNotify(value);
                SendEvent(changeEvent);
            }
        }

        public class ItemData
        {
            public string Name;
            public string Detail;
            public int Flag;
            public ItemData() { }

            public ItemData(string name, int flag)
            {
                Name = name;
                Flag = flag;
            }
        }

        public SelectBoxField Set(string label, Enum v)
        {
            return Set(label, Convert.ToInt32(v), v.GetType());
        }

        public SelectBoxField Set(string label, int v, Type enumType)
        {
            Label = label;
            mValue = v;
            IsSingleFlag = !enumType.IsDefined(typeof(FlagsAttribute));
            var enumValues = Enum.GetValues(enumType);
            var list = new List<ItemData>(enumValues.Length);
            for (var i = 0; i < enumValues.Length; i++)
            {
                var enumName = Enum.GetName(enumType, enumValues.GetValue(i));
                var enumValue = Convert.ToInt32(enumValues.GetValue(i));
                if ((!IsSingleFlag && enumValue == 0) || enumName!.StartsWith("__")) continue;
                var comment = enumType.GetField(enumName)?.GetCustomAttribute<CommentAttribute>(false);
                list.Add(new ItemData { Flag = enumValue, Name = comment?.Name ?? enumName, Detail = comment?.Detail });
            }
            Items = list.ToArray();
            DisplayUI();
            return this;
        }

        public SelectBoxField Set(string label, IEnumerable<ItemData> items, bool isSingleFlag = false)
        {
            return Set(label, 0, items, isSingleFlag);
        }

        public SelectBoxField Set(string label, int v, IEnumerable<ItemData> items, bool isSingleFlag = false)
        {
            IsSingleFlag = isSingleFlag;
            Label = label;
            mValue = v;
            Items = items;
            DisplayUI();
            return this;
        }


        public void DisplayUI()
        {
            Clear();
            style.marginLeft = 2;
            var bar = new Toolbar();
            if (!string.IsNullOrEmpty(Label))
                bar.Add(new Label(Label) { style = { unityTextAlign = TextAnchor.MiddleLeft } });
            bar.Add(Button = new ToolbarButton(PopupAllFlags) { text = GetValueNames(mValue) });
            Button.style.color = new Color(0.6f, 0.6f, 0.6f);
            Button.style.overflow = Overflow.Hidden;
            Button.style.unityTextOverflowPosition = TextOverflowPosition.Middle;
            Button.style.textOverflow = TextOverflow.Ellipsis;
            Button.style.flexGrow = 1;
            Add(bar);
        }


        public string GetValueNames(int flags)
        {
            var strbuf = new StringBuilder();
            var isFirst = true;
            foreach (var item in Items)
            {
                if (IsSingleFlag)
                {
                    if (item.Flag == flags)
                    {
                        strbuf.Append(item.Name);
                        isFirst = false;
                        break;
                    }
                }
                else if ((item.Flag & flags) != 0)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        strbuf.Append('|');
                    }
                    strbuf.Append(item.Name);
                }
            }
            if (isFirst) strbuf.Append("未选择");
            return strbuf.ToString();
        }


        protected virtual void PopupAllFlags()
        {
            var menu = new GenericMenu();
            if (!IsSingleFlag)
            {
                menu.AddItem(new GUIContent("[清除全部]"), false, () => { value = 0; });
                menu.AddItem(new GUIContent("[选择全部]"), false, () =>
                {
                    foreach (var item in Items)
                        value |= item.Flag;
                });
            }
            foreach (var item in Items)
            {
                var isSelected = IsSingleFlag ? item.Flag == mValue : (item.Flag & mValue) != 0;
                menu.AddItem(new GUIContent(item.Name, $"{item.Flag}\n{item.Detail}"), isSelected, flag => value = IsSingleFlag ? (int)flag : mValue ^ (int)flag, item.Flag);
            }
            menu.ShowAsContext();
        }

        public void SetValueWithoutNotify(int newValue)
        {
            mValue = newValue;
            Button.text = GetValueNames(newValue);
            MarkDirtyRepaint();
        }
    }
}
