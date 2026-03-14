//==================={By Qcbf|qcbf@qq.com|6/4/2021 12:25:11 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FLib.Unity.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class NodeStageEditor : VisualElement
    {
        public ShortcutKeyManager ShortcutKey;
        public UndoCommander Undo;

        public NodeStageBackgroundIMGUIDrawer BackgroundDrawer;
        public NodeStageDraggerLayer NodeLayer;
        public NodeStageFrontIMGUIDrawer ForegroundDrawer;
        public NodePropertyPanelEditor PropertyPanel;

        public Dictionary<uint, NodeEditor> Nodes = new();
        public List<INodeSelectableEditor> Selecteds = new();

        internal NodeTempLineEditor TempLine;
        protected MoveNodeCommand mMoveNodeCommand;

        protected bool mIsMouseDown;
        protected bool mIsDraging;

        internal uint UidGen;


        public NodeStageEditor()
        {
            focusable = true;
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.flib.unity/Editor/NodeEditor/Res/Styles.uss"));
            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.flib.unity/Editor/NodeEditor/Res/StylesDark.uss"));
            style.width = new Length(100, LengthUnit.Percent);
            style.height = new Length(100, LengthUnit.Percent);

            Undo = new UndoCommander(this);
            Undo.OnDoEvent = Undo.OnUndoEvent = Undo.OnRedoEvent = command => PropertyPanel.Refresh();

            ShortcutKey = new ShortcutKeyManager();
            RegisterKeys();

            Add(BackgroundDrawer = new NodeStageBackgroundIMGUIDrawer(this));
            Add(NodeLayer = new NodeStageDraggerLayer(this));
            Add(ForegroundDrawer = new NodeStageFrontIMGUIDrawer(this));
            Add(PropertyPanel = CreatePropertyPanel());

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<WheelEvent>(OnStageSizeChange);
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<KeyUpEvent>(OnKeyUp, TrickleDown.TrickleDown);
            Initialize();
        }

        protected virtual async void Initialize()
        {
            await Task.Yield();
            PropertyPanel.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual NodePropertyPanelEditor CreatePropertyPanel()
        {
            return new NodePropertyPanelEditor(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void RegisterKeys()
        {
            ShortcutKey.Register("undo", KeyCode.Z, ShortcutKeyManager.EModifier.Ctrl, Undo.Undo);
            ShortcutKey.Register("redo", KeyCode.Y, ShortcutKeyManager.EModifier.Ctrl, Undo.Redo);
            ShortcutKey.Register("copy", KeyCode.C, ShortcutKeyManager.EModifier.Ctrl, OnCopySelected);
            ShortcutKey.Register("paste", KeyCode.V, ShortcutKeyManager.EModifier.Ctrl, OnPasteSelected);
            ShortcutKey.Register("delete", KeyCode.Delete, ShortcutKeyManager.EModifier.None, () =>
            {
                Undo.Do<AddOrRemoveTargetsCommand>().Finish(false, Selecteds.ToArray());
                ClearSelects();
            });
            ShortcutKey.Register("select all", KeyCode.A, ShortcutKeyManager.EModifier.Ctrl, () => SetSelects(Nodes.Values, true));
        }

        private void OnKeyUp(KeyUpEvent e)
        {
            if (e.keyCode is KeyCode.LeftAlt or KeyCode.RightAlt)
            {
                foreach (var node in Nodes)
                {
                    node.Value.TopFloatUI.visible = false;
                    node.Value.TopFloatUI.text = string.Empty;
                }
                e.StopPropagation();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode is KeyCode.LeftAlt or KeyCode.RightAlt)
            {
                foreach (var node in Nodes)
                {
                    node.Value.TopFloatUI.visible = true;
                    node.Value.TopFloatUI.text = node.Key.ToString();
                }
                e.StopPropagation();
            }
            else if (ShortcutKey.InputKey(panel, e.keyCode, e.ctrlKey, e.shiftKey, e.altKey))
            {
                e.StopPropagation();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnStageSizeChange(WheelEvent e)
        {
            if (PropertyPanel.visible && PropertyPanel.worldBound.Contains(e.mousePosition))
            {
                return;
            }
            var delta = 0.1f;
            NodeLayer.Scale = Mathf.Clamp(NodeLayer.Scale - (e.delta.y > 0 ? delta : -delta), delta, 1.4f);
            BackgroundDrawer.MarkDirtyRepaint();
            PropertyPanel.Refresh();
        }

        protected virtual void OnPasteSelected()
        {
            throw new NotSupportedException();
        }

        protected virtual void OnCopySelected()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnMouseDown(MouseDownEvent e)
        {
            if (PropertyPanel.visible && PropertyPanel.worldBound.Contains(e.mousePosition))
            {
                return;
            }
            ForegroundDrawer.RectSelector.size = Vector2.zero;
            mIsMouseDown = true;
            if (e.button != 2)
            {
                //none, nodeBody, nodeArrow, nodeLine
                var pickType = 0;
                INodeSelectableEditor picked = null;
                var mousePosition = e.mousePosition;
                foreach (var node in Nodes.Values)
                {
                    if (node.BodyUI.worldBound.Contains(mousePosition))
                    {
                        pickType = 1;
                        picked = node;
                        break;
                    }
                    else if (node.ArrowUI.worldBound.Contains(mousePosition))
                    {
                        pickType = 2;
                        picked = node;
                        break;
                    }
                    else
                    {
                        foreach (var line in node.Lines)
                        {
                            if (line.IsContainPoint(mousePosition))
                            {
                                pickType = 3;
                                picked = line;
                                break;
                            }
                        }
                    }
                }

                if (pickType == 0)
                {
                    ClearSelects();
                    ForegroundDrawer.RectSelector.position = e.localMousePosition;
                    ForegroundDrawer.RectSelector.size = Vector2.one * 0.1f;
                }
                else if (pickType is 1 or 3)
                {
                    if (!Selecteds.Contains(picked))
                    {
                        SetSelects(new[] { picked }, true);
                    }
                }
                else
                {
                    TempLine = new NodeTempLineEditor((NodeEditor)picked) { RightPoint = mousePosition };
                }
            }
            e.StopPropagation();
            Focus();
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnMouseMove(MouseMoveEvent evt)
        {
            if (!mIsMouseDown)
            {
                return;
            }
            if (evt.pressedButtons == 1)
            {
                mIsDraging = true;
                if (ForegroundDrawer.IsHaveRectSelector)
                {
                    ForegroundDrawer.RectSelector.size += evt.mouseDelta;
                }
                else if (TempLine != null)
                {
                    var picked = Nodes.Values.SingleOrDefault(v => v.BodyUI.worldBound.Contains(evt.mousePosition));
                    if (picked == null || picked == TempLine.Left || TempLine.Left.Lines.Any(v => v.Right == picked))
                    {
                        TempLine.RightPoint = evt.mousePosition;
                    }
                    else
                    {
                        TempLine.Line.RightUid = picked.Uid;
                    }
                }
                else
                {
                    mMoveNodeCommand ??= Undo.Do<MoveNodeCommand>().SetData(Selecteds.Where(v => v is NodeEditor).Cast<NodeEditor>());
                    foreach (var item in Selecteds)
                    {
                        if (item is NodeEditor node)
                        {
                            node.Position += evt.mouseDelta;
                        }
                    }
                }
                ForegroundDrawer.MarkDirtyRepaint();
            }
            else if (evt.pressedButtons == 4)
            {
                NodeLayer.Position += evt.mouseDelta;
                PropertyPanel.Refresh();
            }
            ForegroundDrawer.MarkDirtyRepaint();
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.target == PropertyPanel) return;
            if (evt.button == 0)
            {
                if (mIsDraging)
                {
                    if (ForegroundDrawer.IsHaveRectSelector)
                    {
                        var rect = ForegroundDrawer.RectSelector;
                        ForegroundDrawer.RectSelector = default;
                        OnRectSelectNodes(rect);
                    }
                    else if (mMoveNodeCommand != null)
                    {
                        mMoveNodeCommand.Finish();
                        mMoveNodeCommand = null;
                    }
                    else if (TempLine != null)
                    {
                        Undo.Do<CreateLineCommand>().Finish(TempLine.Line, NodeLayer.NodeContainer.WorldToLocal(evt.mousePosition));
                        TempLine = null;
                    }
                }
                else if (!mIsDraging && Selecteds.Count > 1)
                {
                    var picked = Nodes.Values.SingleOrDefault(v => v.BodyUI.worldBound.Contains(evt.mousePosition));
                    if (picked != null)
                    {
                        SetSelects(new[] { picked }, true);
                    }
                    else
                    {
                        ClearSelects();
                    }
                }
            }
            else if (evt.button == 1)
            {
                var menu = new GenericMenu();
                CreateContextMenu(NodeLayer.NodeContainer.WorldToLocal(evt.mousePosition), menu);
                menu.ShowAsContext();
            }
            mIsDraging = mIsMouseDown = false;
            evt.StopPropagation();
            ForegroundDrawer.MarkDirtyRepaint();
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnRectSelectNodes(Rect rect)
        {
            rect = VisualElementExtensions.LocalToWorld(this, rect);
            SetSelects(Nodes.Values.Where(v => rect.Overlaps(v.BodyUI.worldBound, true)), true);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void CreateContextMenu(Vector2 mousePosition, GenericMenu menu)
        {
            if (Selecteds.Count > 0)
            {
                var nodes = Selecteds.Where(v => v is NodeEditor).ToArray();
                if (nodes.Length > 0)
                {
                    menu.AddItem(new GUIContent("克隆节点"), false, null);
                }
                menu.AddItem(new GUIContent("删除选中目标"), false, () => Undo.Do<AddOrRemoveTargetsCommand>().Finish(false, Selecteds.ToArray()));
            }
            else
            {
                menu.AddItem(new GUIContent("添加节点"), false, () => Undo.Do<AddOrRemoveTargetsCommand>().Finish(true, new[] { CreateNode(mousePosition) }));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual NodeEditor CreateNode(Vector2 position, object state = null)
        {
            return new NodeEditor(this, GenUid()) { FormatPosition = position };
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual NodeLineEditor CreateLine(NodeEditor left)
        {
            return new NodeLineEditor(left.Stage, left);
        }

        public uint GenUid()
        {
            uint uid;
            do
            {
                uid = ++UidGen;
            } while (Nodes.ContainsKey(uid));
            return uid;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void AddTargets(IEnumerable<INodeSelectableEditor> targets)
        {
            foreach (var target in targets)
            {
                switch (target)
                {
                    case NodeEditor node:
                        NodeLayer.Add(node);
                        Nodes.Add(node.Uid, node);
                        break;
                    case NodeLineEditor line:
                        line.AddToLeft();
                        break;
                }
            }
            PropertyPanel.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void RemoveTargets(IEnumerable<INodeSelectableEditor> targets)
        {
            foreach (var target in targets)
            {
                switch (target)
                {
                    case NodeEditor node:
                        Nodes.Remove(node.Uid);
                        NodeLayer.Remove(node);
                        break;
                    case NodeLineEditor line:
                        line.RemoveFromLeft();
                        break;
                }
            }
            ForegroundDrawer.MarkDirtyRepaint();
            PropertyPanel.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void SetSelects(IEnumerable<INodeSelectableEditor> nodes, bool v)
        {
            if (v)
            {
                foreach (var item in Selecteds)
                {
                    item.IsSelected = false;
                }
                Selecteds.Clear();
                Selecteds.AddRange(nodes);
            }
            else
            {
                Selecteds.RemoveAll(nodes.Contains);
            }
            foreach (var item in nodes)
            {
                item.IsSelected = v;
            }
            PropertyPanel.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void AddSelects(IEnumerable<INodeSelectableEditor> nodes)
        {
            Selecteds.AddRange(nodes);
            foreach (var item in nodes)
            {
                item.IsSelected = true;
            }
            PropertyPanel.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearSelects()
        {
            foreach (var item in Selecteds)
            {
                item.IsSelected = false;
            }
            Selecteds.Clear();
            PropertyPanel.Refresh();
        }

        public new virtual void Clear()
        {
            ClearSelects();
            NodeLayer.Clear();
            Undo.Clear();
            Nodes.Clear();
            TempLine = null;
            mMoveNodeCommand = null;
            mIsMouseDown = mIsDraging = false;
            UidGen = 0;
        }


        public virtual Vector2 WorldToLocal(Vector2 v)
        {
            return NodeLayer.WorldToLocal(v);
        }

        public virtual Rect WorldToLocal(Rect v)
        {
            return NodeLayer.WorldToLocal(v);
        }

        public virtual Vector2 LocalToWorld(Vector2 v)
        {
            return NodeLayer.LocalToWorld(v);
        }

        public virtual Rect LocalToWorld(Rect v)
        {
            return NodeLayer.LocalToWorld(v);
        }
    }
}
