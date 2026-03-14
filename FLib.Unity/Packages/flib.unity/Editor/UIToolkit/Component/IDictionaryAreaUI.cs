using FLib.Unity.Editor;
using System.Linq;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
public class IDictionaryAreaUI : TitleAreaUI
{
    public Action<ItemContainer, ItemContainer> OnSwapItem;
    public Func<bool, ItemContainer, VisualElement> ValueCreatorCallback;
    public Func<ItemContainer, bool> ValueDeleterCallback;
    public bool IsDisplayUp;
    public Object UndoObject;

    public bool IsEditable { get; private set; }

    public class ItemContainer : VisualElement
    {
        public IDictionaryAreaUI Root;
        public Label LabelUI;
        public Button UpBtnUI;
        public Button DelBtnUI;
        public int Index;

        public ItemContainer(IDictionaryAreaUI root)
        {
            Root = root;
            style.flexDirection = FlexDirection.Row;
            style.flexGrow = 1;
            Add(LabelUI = new Label("").TextAlign(TextAnchor.MiddleLeft).MinWidth(12).Color(new Color(0.4f, 0.4f, 0.4f)));
            if (root.IsEditable)
            {

                Add(DelBtnUI = new ToolbarButton(Delete) { text = "Del" }.FlexShrink(0));
                DelBtnUI.style.alignItems = Align.FlexEnd;
            }
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



        public void Refresh()
        {
            Index = Root.IndexOf(this);
            UpBtnUI?.SetEnabled(Index > 0);
            //LabelUI.text = Index.ToString();
        }
    }


    public IDictionaryAreaUI(Func<bool, ItemContainer, VisualElement> creator, Func<ItemContainer, bool> deleter, int itemCount = 0, bool isEditable = true, Object undoObject = null)
    {
        UndoObject = undoObject;
        IsEditable = isEditable;
        ValueCreatorCallback = creator;
        ValueDeleterCallback = deleter;
        AddUI();
        RegisterCallback<GeometryChangedEvent>(OnGeometryChangeEvent);
        RefreshItems(itemCount);
    }

    //public IDictionaryAreaUI(bool isEditable = true)
    //{
    //    IsEditable = isEditable;

    //    AddUI();
    //}


    private void OnGeometryChangeEvent(GeometryChangedEvent evt)
    {
        foreach (var item in Children())
        {
            (item as ItemContainer)?.Refresh();
        }
    }

    protected void AddUI()
    {
        this.Border(value ? new Color(0.3f, 0.3f, 0.3f) : Color.clear);
        if (IsEditable)
        {
            AddToMenuBar(new ToolbarButton(OnClickAddItem) { text = "+" });
        }
    }

    public override void SetValueWithoutNotify(bool newValue)
    {
        base.SetValueWithoutNotify(newValue);
        this.Border(newValue ? new Color(0.3f, 0.3f, 0.3f) : Color.clear);
    }

    private void OnClickAddItem()
    {
        value = true;
        CreateItem(true);
    }

    /// <summary>
    /// 
    /// </summary>
    public ItemContainer CreateItem(bool isNewData = false, object userData = null)
    {
        var container = new ItemContainer(this) { Index = childCount, userData = userData };
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
                container.Insert(1, result.FlexGrow(1));
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
    public IDictionaryAreaUI RefreshItems(int? count = null)
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
    public IDictionaryAreaUI AcceptDrag(Type type)
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
