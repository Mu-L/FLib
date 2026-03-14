//==================={By Qcbf|qcbf@qq.com|8/4/2021 3:16:01 PM}===================

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class EditorListChooser : EditorWindow
    {
        public Action<int, string> OnSelectEvent;

        public string[] Items;
        public HashSet<string> DisableItems;
        public HashSet<string> ChoosedItems;

        private (int, string) mSelected;


        public static EditorListChooser Open(string[] items, Action<int, string> onSelectEvent)
        {
            var wnd = EditorFLibUtility.OpenWindowToCursor<EditorListChooser>();
            wnd.Items = items;
            wnd.OnSelectEvent = onSelectEvent;
            return wnd;
        }

        public static void OpenWithDisplay(string[] items, Action<int, string> onSelectEvent)
        {
            var wnd = EditorFLibUtility.OpenWindowToCursor<EditorListChooser>();
            wnd.Items = items;
            wnd.OnSelectEvent = onSelectEvent;
            wnd.DisplayUI();
        }

        private void OnDestroy()
        {
            OnSelectEvent?.Invoke(mSelected.Item1, mSelected.Item2);
        }


        public EditorListChooser DisplayUI()
        {
            var search = new ToolbarSearchField { style = { width = StyleKeyword.Initial } };
            search.RegisterValueChangedCallback(_ =>
            {
                rootVisualElement.RemoveAt(rootVisualElement.childCount - 1);
                rootVisualElement.Add(CreateItemsUI(search.value));
            });
            rootVisualElement.Add(search);
            rootVisualElement.Add(CreateItemsUI(string.Empty));
            return this;
        }


        private VisualElement CreateItemsUI(string match)
        {
            var itemsContainer = new ScrollView();
            for (var i = 0; i < Items.Length; i++)
            {
                if (Items[i].Contains(match))
                {
                    var ui = CreateItemUI(i, ChoosedItems?.Contains(Items[i]) == true);
                    if (DisableItems?.Contains(Items[i]) == true)
                    {
                        ui.SetEnabled(false);
                    }

                    itemsContainer.Add(ui);
                }
            }

            return itemsContainer;
        }


        private VisualElement CreateItemUI(int index, bool isChoosed)
        {
            var btn = new Button(() =>
                {
                    mSelected = (index, Items[index]);
                    Close();
                })
                { text = Items[index] };
            if (isChoosed)
            {
                btn.style.color = Color.green;
            }

            return btn;
        }
    }
}
