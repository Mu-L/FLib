//==================={By Qcbf|qcbf@qq.com|12/10/2021 6:17:10 PM}===================

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class ListAreaUI : TitleAreaUI
    {
        public Action<int, int> OnSwapItem;
        public Func<bool, ItemContainer, VisualElement> ValueCreatorCallback;
        public Func<ItemContainer, bool> ValueDeleterCallback;
        public bool IsDisplayUp;
        public Object UndoObject;

        public bool IsEditable { get; private set; }

        public class ItemContainer : VisualElement
        {
            public ListAreaUI Root;
            public Label LabelUI;
            public Button UpBtnUI;
            public Button DelBtnUI;
            public int Index;

            public ItemContainer(ListAreaUI root)
            {
                Root = root;
                style.flexDirection = FlexDirection.Row;
                style.flexGrow = 1;
                style.borderRightWidth = style.borderTopWidth = style.borderLeftWidth = style.borderBottomWidth = 0;
                style.borderRightColor = style.borderTopColor = style.borderLeftColor = style.borderBottomColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

                Add(LabelUI = new Label("0").TextAlign(TextAnchor.MiddleLeft).MinWidth(12).Color(new Color(0.4f, 0.4f, 0.4f)));
                RegisterCallback<MouseEnterEvent>(OnMouseEnterEvent);
                RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
                if (root.IsEditable)
                    LabelUI.RegisterCallback<ContextClickEvent>(OnContextClick);
            }

            private void OnMouseLeaveEvent(MouseLeaveEvent evt)
            {
                style.borderRightWidth = style.borderTopWidth = style.borderLeftWidth = style.borderBottomWidth = 0;
            }

            private void OnMouseEnterEvent(MouseEnterEvent evt)
            {
                style.borderRightWidth = style.borderTopWidth = style.borderLeftWidth = style.borderBottomWidth = 1;
            }

            private void OnContextClick(ContextClickEvent evt)
            {
                var menu = new GenericMenu();
                if (Root.IsDisplayUp)
                    menu.AddItem(new GUIContent("上移"), false, Up);
                menu.AddItem(new GUIContent("删除"), false, Delete);
                menu.ShowAsContext();
            }

            public void Delete()
            {
                if (Root.UndoObject != null)
                {
                    Undo.RecordObject(Root.UndoObject, "delete item");
                    EditorUtility.SetDirty(Root.UndoObject);
                }
                if (Root.ValueDeleterCallback?.Invoke(this) != false)
                    RemoveFromHierarchy();
            }

            public void Up()
            {
                var i = Root.IndexOf(this);
                Root.RemoveAt(i);
                Root.Insert(i - 1, this);
                Refresh();
                ((ItemContainer)Root[i]).Refresh();
                Root.OnSwapItem?.Invoke(i, Index);
            }

            public void Refresh()
            {
                Index = Root.IndexOf(this);
                UpBtnUI?.SetEnabled(Index > 0);
                LabelUI.text = Index.ToString();
            }
        }


        public ListAreaUI(Func<bool, ItemContainer, VisualElement> creator, Func<ItemContainer, bool> deleter, int itemCount = 0, bool isEditable = true, Object undoObject = null, string autoFoldoutKey = null) : base(autoFoldoutKey)
        {
            UndoObject = undoObject;
            IsEditable = isEditable;
            ValueCreatorCallback = creator;
            ValueDeleterCallback = deleter;
            AddUI();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChangeEvent);
            RefreshItems(itemCount);
        }

        private void OnGeometryChangeEvent(GeometryChangedEvent evt)
        {
            foreach (var item in Children())
            {
                (item as ItemContainer)?.Refresh();
            }
        }

        protected void AddUI()
        {
            style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderBottomColor = style.borderLeftColor = style.borderRightColor = value ? new Color(0.3f, 0.3f, 0.3f) : Color.clear;
            if (IsEditable)
            {
                AddToMenuBar(new ToolbarButton(OnClickAddItem) { text = "+" });
            }
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderBottomColor = style.borderLeftColor = style.borderRightColor = newValue ? new Color(0.3f, 0.3f, 0.3f) : Color.clear;
        }

        private void OnClickAddItem()
        {
            value = true;
            CreateItem(true);
        }

        /// <summary>
        /// 
        /// </summary>
        public ItemContainer CreateItem(bool isNewData = false, object setUserData = null)
        {
            var container = new ItemContainer(this) { Index = childCount, userData = setUserData };
            Add(container);
            if (ValueCreatorCallback != null)
            {
                if (isNewData && UndoObject != null)
                {
                    Undo.RecordObject(UndoObject, "create item");
                    EditorUtility.SetDirty(UndoObject);
                }
                var result = ValueCreatorCallback.Invoke(isNewData, container);
                if (result != null)
                {
                    container.Insert(1, result);
                    return container;
                }
                Remove(container);
                return null;
            }

            return container;
        }

        /// <summary>
        /// 
        /// </summary>
        public ListAreaUI RefreshItems(int? count = null)
        {
            count ??= childCount;
            Clear();
            for (var i = 0; i < count; i++)
                CreateItem();
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public ListAreaUI AcceptDrag(Type type)
        {
            MenuBarUI.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (!DragAndDrop.objectReferences.Any(v => v != null && type.IsInstanceOfType(v)))
                    return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                e.StopPropagation();
            });
            MenuBarUI.RegisterCallback<DragPerformEvent>(e =>
            {
                DragAndDrop.AcceptDrag();
                e.StopPropagation();
                foreach (var obj in DragAndDrop.objectReferences)
                    if (obj != null && type.IsInstanceOfType(obj))
                        CreateItem(true, obj);
            });
            return this;
        }
    }
}
