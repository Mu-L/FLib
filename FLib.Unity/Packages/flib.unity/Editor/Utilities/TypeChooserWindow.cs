//==================={By Qcbf|qcbf@qq.com|8/20/2021 3:44:16 PM}===================

using FLib;
using FLib.Unity.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FLib.Unity.Pinyin;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class TypeChooserWindow : EditorWindow
    {
        public Action<Type> OnSelectEvent;
        public Type[] Items;
        public Type Selected;
        public EOption Options;

        [Flags]
        public enum EOption
        {
            None,
            HideSetNull = 0x1,
            AppendNamespace = 0x2,
            ContainBaseType = 0x4,
            MustComment = 0x8,
        }

        public static TypeChooserWindow Open(Type baseType, Type selected = null, Action<Type> onSelect = null, Func<Type, bool> filter = null, EOption options = EOption.None)
        {
            var wnd = EditorFLibUtility.OpenWindowToCursor<TypeChooserWindow>();
            wnd.Options = options;
            wnd.titleContent = new GUIContent((baseType != null ? baseType.GetCustomAttribute<CommentAttribute>(false)?.Name ?? baseType.Name : string.Empty) + " 脚本选择器");
            wnd.Selected = selected;
            wnd.Items = (from t in EditorFLibUtility.UserAssemblyTypes.AsParallel()
                where !t.IsAbstract && !t.IsInterface
                where (options & EOption.ContainBaseType) != 0 || t != baseType
                where baseType?.IsAssignableFrom(t) != false
                where filter?.Invoke(t) != false
                orderby t.Name
                select t).ToArray();
            wnd.OnSelectEvent = onSelect;
            wnd.DisplayUI();
            return wnd;
        }

        /// <summary>
        /// 
        /// </summary>
        public TypeChooserWindow Modal()
        {
            ShowModalUtility();
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TryGetSelected(out Type val, bool toModal = true)
        {
            if (toModal)
                Modal();
            val = Selected;
            return Selected != null;
        }

        /// <summary>
        /// 
        /// </summary>
        private void DisplayUI()
        {
            var search = new ToolbarSearchField();
            rootVisualElement.Add(search);
            var content = new ScrollView(ScrollViewMode.Vertical);
            rootVisualElement.Add(content);

            search.style.width = StyleKeyword.Initial;
            search.RegisterValueChangedCallback(e => RefreshContentUI());

            RefreshContentUI();
            return;

            void RefreshContentUI()
            {
                content.Clear();
                if ((Options & EOption.HideSetNull) == 0)
                {
                    content.Add(new Button(() =>
                        {
                            Selected = null;
                            Close();
                            OnSelectEvent?.Invoke(Selected);
                        })
                        { text = "置空" });
                }

                for (var i = 0; i < Items.Length; i++)
                {
                    var item = Items[i];
                    if ((Options & EOption.MustComment) != 0 && !item.IsDefined(typeof(CommentAttribute)))
                        continue;
                    var itemName = CommentAttribute.TryGetLabel(item, out var detail, appendTypeNameToDetail: false);
                    detail = ((Options & EOption.AppendNamespace) != 0 ? $"{item.Namespace}.{item.Name}\n" : $"{item.Name}\n") + detail;
                    if (string.IsNullOrWhiteSpace(search.value) || itemName.ToLowerInvariant().Contains(search.value) || PinyinHelper.GetPinyinInitials(itemName).Contains(search.value, StringComparison.OrdinalIgnoreCase))
                    {
                        var btn = new Button
                        {
                            userData = i,
                            text = itemName,
                            tooltip = detail,
                            style = { unityTextAlign = TextAnchor.MiddleLeft }
                        };
                        if (item == Selected)
                            btn.SetEnabled(false);
                        btn.RegisterCallback<ClickEvent>(e =>
                        {
                            Selected = Items[(int)((VisualElement)e.currentTarget).userData];
                            Close();
                            OnSelectEvent?.Invoke(Selected);
                        });
                        content.Add(btn);
                    }
                }
            }
        }
    }
}
