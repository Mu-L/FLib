// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using FLib.WorldCores.TimeLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class ClipItemPanel : VisualElement, ISelectable, IContextMenuProcessable
    {
        public readonly ClipAreaPanel ParentPanel;
        public TimeLogicClip Clip;
        public StagePanel Stage => ParentPanel.Stage;
        public FEvent.PostEventHandler<FEventValue<int>.ChangeEvent> OnFrameChangeEventHandler;

        public Label Label;

        private float _dragDeltaX;

        public object InspectorValue => Clip;

        public ClipItemPanel(ClipAreaPanel parentPanel, TimeLogicClip clip)
        {
            focusable = true;
            style.position = Position.Absolute;
            style.bottom = style.top = 0;

            ParentPanel = parentPanel;
            Clip = clip;
            style.backgroundColor = Color.gray;
            Add(Label = new Label(clip.Name)
            {
                style =
                {
                    fontSize = 11, flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter,
                    overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, unityTextOverflowPosition = TextOverflowPosition.Middle
                }
            });

            Add(new VisualElement() { style = { backgroundColor = Color.white * 0.9f, height = 3 } });
            Add(CreateArrowUI(true));
            Add(CreateArrowUI(false));

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            this.RegisterDragSelf(evt =>
            {
                if (!TryDragFrameDelta(evt.deltaPosition.x, out var deltaFrame))
                    return;
                var frameBegin = Clip.BeginFrame + deltaFrame;
                if (frameBegin < 0)
                    return;
                Clip.BeginFrame = frameBegin;
                Clip.EndFrame += deltaFrame;
                RefreshFrameRange();
            }, onBeginDrag: (_, _) => _dragDeltaX = 0);


            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }

        protected virtual void OnAttachToPanelEvent(AttachToPanelEvent _)
        {
            RefreshUI();
            if (Clip != null) Utility.TryCallMethod(Clip, this, Clip.GetType().GetCustomAttribute<TimeLogicEditorAttribute>()?.EnterPreviewMethod);
            if (OnFrameChangeEventHandler != null)
            {
                Stage.CurrentFrame.ListenEvent(OnFrameChangeEventHandler);
                OnFrameChangeEventHandler(null, new FEventValue<int>.ChangeEvent(Stage.CurrentFrame, Stage.CurrentFrame));
            }
        }

        protected virtual void OnDetachFromPanelEvent(DetachFromPanelEvent _)
        {
            if (OnFrameChangeEventHandler != null)
                Stage.CurrentFrame.UnlistenEvent(OnFrameChangeEventHandler);
            if (Clip != null) Utility.TryCallMethod(Clip, this, Clip.GetType().GetCustomAttribute<TimeLogicEditorAttribute>()?.ExitPreviewMethod);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual VisualElement CreateArrowUI(bool isLeft)
        {
            var el = new VisualElement() { style = { backgroundColor = Color.gray * 1.2f, position = Position.Absolute, width = 5, top = 0, bottom = 3, cursor = UIToolkitUtility.GetCursor(MouseCursor.ResizeHorizontal) } };
            if (isLeft)
                el.style.left = 0;
            else
                el.style.right = 0;
            el.RegisterDragSelf(evt =>
            {
                if (!TryDragFrameDelta(evt.deltaPosition.x, out var deltaFrame))
                    return;
                if (isLeft)
                {
                    var frame = Clip.BeginFrame + deltaFrame;
                    if (frame >= 0 && frame < Clip.EndFrame)
                    {
                        Clip.BeginFrame = frame;
                        RefreshFrameRange();
                    }
                }
                else
                {
                    var frame = Clip.EndFrame + deltaFrame;
                    if (frame >= Clip.BeginFrame)
                    {
                        Clip.EndFrame = frame;
                        RefreshFrameRange();
                    }
                }
            }, onBeginDrag: (_, _) => _dragDeltaX = 0, onDown: (_, downEvent) => downEvent.StopPropagation());
            return el;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (evt.button > 1) return;
            _dragDeltaX = 0;
            if (evt.clickCount == 2)
                Stage.OpenInspectorPanel(this);
            else
                Stage.Selected.Value = this;
            evt.StopPropagation();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnSelectChange(bool value)
        {
            style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = style.borderBottomWidth = value ? 1 : 0;
            style.borderLeftColor = style.borderRightColor = style.borderTopColor = style.borderBottomColor = value ? Color.white : StyleKeyword.Null;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void RefreshUI()
        {
            Label.text = Clip.Name;
            Label.style.color = Clip.IsDisable ? Color.gray * 0.6f : Color.white * 0.9f;
            RefreshFrameRange();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void RefreshFrameRange()
        {
            style.left = GetClipX(Clip.BeginFrame);
            style.width = Stage.FrameGraphicInterval * Clip.FrameCount - Stage.FrameGraphicInterval * 0.25f;
            tooltip = $"从{Clip.BeginFrame}帧 到{Clip.EndFrame}帧 总共{Clip.FrameCount}帧";
            if (panel != null)
                Stage.RefreshEndFrame();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ContextMenuProcess(MouseUpEvent evt, GenericMenu menu)
        {
            if (Clip is IContextMenuProcessable clipMenu)
                clipMenu.ContextMenuProcess(evt, menu);

            menu.AddItem(new GUIContent($"删除片段[{Clip.Name}]"), false, () =>
            {
                ArrayFLibUtility.Remove(ref ParentPanel.ParentPanel.Track.Clips, Clip);
                RemoveFromHierarchy();
                Utility.RemoveUnityObjectStoreRef(Clip, Stage.Runtime.ExternalReferences);
            });
            menu.AddItem(new GUIContent($"复制片段[{Clip.Name}]"), false, () =>
            {
                var text = $"{nameof(TimeLogicClip)}|{Clip.Name}|{TypeAssistant.GetTypeName(Clip.GetType())}|" + Convert.ToBase64String(BytesPack.Pack(Clip));
                EditorFLibUtility.ClipboardTxt = text;
                Stage.Tips.Show($"复制片段[{Clip.Name}]");
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool TryDragFrameDelta(float deltaX, out int deltaFrame)
        {
            _dragDeltaX += deltaX / Stage.FrameGraphicInterval;
            deltaFrame = (int)_dragDeltaX;
            if (deltaFrame == 0)
                return false;
            _dragDeltaX -= deltaFrame;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual float GetClipX(int frame)
        {
            return ParentPanel.contentRect.x + frame * Stage.FrameGraphicInterval;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnSave(bool isPre)
        {
        }
    }
}
