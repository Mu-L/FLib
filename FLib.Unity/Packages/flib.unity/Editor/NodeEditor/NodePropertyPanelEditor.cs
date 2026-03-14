//==================={By Qcbf|qcbf@qq.com|6/13/2021 11:33:01 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class NodePropertyPanelEditor : VisualElement
    {
        public static bool Displayed = true;
        public NodeStageEditor Stage;
        protected ScrollView mScroll;


        public NodePropertyPanelEditor(NodeStageEditor stage)
        {
            pickingMode = PickingMode.Ignore;
            name = "PropertyPanel";
            Stage = stage;
            style.width = EditorPrefs.GetFloat("nodePropertyPanelWidth", 300);
            Add(mScroll = new ScrollView(ScrollViewMode.Vertical));
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AddStageInfos()
        {
            //NodeLayer.Position + "|" + NodeLayer.Scale.ToString("0.##") + "|" + Nodes.Count;
            var btn = new Button(() =>
                {
                    Stage.NodeLayer.Position = default;
                    Stage.NodeLayer.Scale = 1;
                    Refresh();
                })
                { text = $"坐标: {Stage.NodeLayer.Position}\t缩放: {Stage.NodeLayer.Scale:0.##}" };
            btn.AddToClassList("LabelButton");
            mScroll.Add(btn);

            btn = new Button(() =>
                {
                    var ui = new FloatField() { value = style.width.value.value };
                    var w = DialogWindow.Open(new DialogWindow.OptionData { Btns = new[] { "取消", "确定" }, CustomUI = ui }, DialogWindow.EOpenType.ModalUtility);
                    if (w.SelectBtnIndex == 1)
                    {
                        EditorPrefs.SetFloat("nodePropertyPanelWidth", Mathf.Clamp((style.width = ui.value).value.value, 60, 1000));
                        Refresh();
                    }
                })
                { text = "面板宽度:" + style.width.value.value };
            btn.AddToClassList("LabelButton");
            mScroll.Add(btn);

            mScroll.Add(btn);
            mScroll.Add(new Label($"节点总数: {Stage.Nodes.Count}\n可撤销次数: {Stage.Undo.DoCommands.Count}"));
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AddContent(VisualElement container, INodeSelectableEditor target)
        {
            switch (target)
            {
                case NodeEditor node:
                    container.Add(CreateNodeContent(node));
                    break;
                case NodeLineEditor line:
                    container.Add(CreateLineContent(line));
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual VisualElement CreateNodeContent(NodeEditor node)
        {
            var area = new VisualElement();
            area.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            area.Add(new Label("Uid:" + node.Uid));

            var titleText = new TextField { value = node.Title };
            titleText.style.flexGrow = 1;
            titleText.RegisterCallback<FocusOutEvent>(e => Stage.Undo.Do<ChangeNodeTitleCommand>().Finish(node, titleText.value));
            area.Add(titleText);
            return area;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual VisualElement CreateLineContent(NodeLineEditor line)
        {
            var area = new VisualElement();
            var bar = new Toolbar() { name = "CommentBar" };
            area.Add(bar);
            var titleText = new TextField("备注") { value = line.Comment }.ShortFieldLabel();
            titleText.style.flexGrow = 1;
            titleText.RegisterCallback<FocusOutEvent>(e => Stage.Undo.Do<ChangeLineCommentCommand>().Finish(line, titleText.value));
            bar.Add(titleText);

            area.Add(bar = new Toolbar() { name = "NodeBar" });
            bar.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (line.Right == null || line.Left == null)
            {
                bar.Add(new Label("无效连接线:" + line.Comment + "|" + line.Left?.Uid + "<>" + line.RightUid));
            }
            else
            {
                bar.Add(CreateLineLRNodeUI(line, true));
                bar.Add(new Label("<>"));
                bar.Add(CreateLineLRNodeUI(line, false));
            }

            area.Add(bar = new Toolbar() { name = "MiscBar" });
            area.style.justifyContent = Justify.SpaceAround;
            var isInvertResultUI = new Toggle() { value = line.IsInvertResult, text = "反转" };
            isInvertResultUI.style.flexGrow = 1;
            isInvertResultUI.RegisterValueChangedCallback(e => Stage.Undo.Do<ChangeLineInvertResultCommand>().Finish(e.newValue, line));
            bar.Add(isInvertResultUI);
            return area;
        }

        /// <summary>
        /// 
        /// </summary>
        private VisualElement CreateLineLRNodeUI(NodeLineEditor line, bool isLeft)
        {
            var root = new VisualElement();
            root.style.flexGrow = 1;
            var node = isLeft ? line.Left : line.Right;
            root.style.flexDirection = FlexDirection.Row;
            var labelUI = new Label() { text = node.Title };
            labelUI.style.width = 100;
            labelUI.style.textOverflow = TextOverflow.Ellipsis;
            labelUI.style.overflow = Overflow.Hidden;
            labelUI.style.unityTextOverflowPosition = TextOverflowPosition.Middle;
            labelUI.style.unityTextAlign = TextAnchor.MiddleCenter;
            labelUI.style.flexGrow = 1;
            var uidUI = new LongField() { value = node.Uid };
            uidUI.style.minWidth = 28;
            uidUI.RegisterCallback<FocusOutEvent>(e =>
            {
                var newUid = (uint)uidUI.value;
                if (!Stage.Nodes.ContainsKey(newUid) || newUid == line.Left.Uid || newUid == line.RightUid)
                {
                    uidUI.value = isLeft ? line.Left.Uid : line.RightUid;
                }
                else if (isLeft)
                {
                    if (line.Left.Uid != uidUI.value) Stage.Undo.Do<ChangeLineConnectComand>().Finish(line, newUid, line.RightUid);
                }
                else
                {
                    if (line.RightUid != uidUI.value) Stage.Undo.Do<ChangeLineConnectComand>().Finish(line, line.Left.Uid, newUid);
                }
            }, TrickleDown.TrickleDown);
            if (isLeft)
            {
                root.Add(uidUI);
                root.Add(labelUI);
            }
            else
            {
                root.Add(labelUI);
                root.Add(uidUI);
            }
            return root;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Refresh()
        {
            mScroll.Clear();
            var count = Math.Min(Stage.Selecteds.Count, 4);
            if (count == 0)
            {
                AddStageInfos();
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var container = new VisualElement { name = "PropertyPanelContent" };
                    AddContent(container, Stage.Selecteds[i]);
                    mScroll.Add(container);
                }
            }
        }
    }
}
