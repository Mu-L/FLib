// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Reflection;
using FLib.WorldCores.TimeLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace FLib.Unity.Editor.TimeLogic
{
    public class TrackItemPanel : VisualElement, IContextMenuProcessable, ISelectable
    {
        public readonly TrackListPanel ParentPanel;
        public readonly TimeLogicTrack Track;
        public ClipAreaPanel ClipAreaPanel;
        public object InspectorValue => Track;
        public StagePanel Stage => ParentPanel.Stage;

        public TrackItemPanel(TrackListPanel parentPanel, TimeLogicTrack track)
        {
            ParentPanel = parentPanel;
            Track = track;
            this.FlexDirection(FlexDirection.Row).Height(24).Margin(2, 2, 0, 0);
            style.borderTopWidth = style.borderBottomWidth = 1;
            style.borderTopColor = style.borderBottomColor = Color.gray;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RefreshUI();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshUI()
        {
            Clear();
            Add(new TrackItemHeadPanel(this));
            Add(ClipAreaPanel = new ClipAreaPanel(this));
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnSelectChange(bool value)
        {
            // style.borderTopColor = style.borderBottomColor = style.borderRightColor = style.borderLeftColor = value ? new Color(0.3f, 0.8f, 0.3f) : Color.gray;
            style.backgroundColor = value ? new StyleColor(Color.gray * 0.8f) : new StyleColor(StyleKeyword.Null);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button > 1) return;
            if (evt.clickCount == 2)
                Stage.OpenInspectorPanel(this);
            else
                Stage.Selected.Value = this;
            evt.StopPropagation();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ContextMenuProcess(MouseUpEvent evt, GenericMenu menu)
        {
            var mouseFrame = Mathf.Max(0, Mathf.RoundToInt(this.Q<ClipAreaPanel>().WorldToLocal(evt.mousePosition).x / Stage.FrameGraphicInterval));
            menu.AddItem(new GUIContent("添加片段"), false, () =>
            {
                var runtimeType = Track.Runtime.GetType();
                var trackName = Track.GetType().FullName!;
                if (!TypeChooserWindow.Open(typeof(TimeLogicClip), options: TypeChooserWindow.EOption.ContainBaseType | TypeChooserWindow.EOption.HideSetNull, filter: t =>
                    {
                        var op = t.GetCustomAttribute<TimeLogicEditorAttribute>();
                        return op == null || (op.RequiredRuntime?.IsAssignableFrom(runtimeType) != false && (string.IsNullOrEmpty(op.TrackTypeNameMatch) || trackName.Contains(op.TrackTypeNameMatch)));
                    }).TryGetSelected(out var type))
                    return;
                var clip = (TimeLogicClip)TypeAssistant.New(type);
                clip.Name = CommentAttribute.TryGetLabel(type);
                clip.BeginFrame = mouseFrame;
                clip.EndFrame = mouseFrame + 5;
                clip.Track = Track;
                ArrayFLibUtility.Add(ref Track.Clips, clip);
                ClipAreaPanel.RefreshClips();
            });
            var copiedText = EditorFLibUtility.ClipboardTxt;
            if (copiedText.StartsWith($"{nameof(TimeLogicClip)}|"))
            {
                var texts = copiedText.Split('|');
                menu.AddItem(new GUIContent($"粘贴片段[{texts[1]}]"), false, () =>
                {
                    var clip = (TimeLogicClip)TypeAssistant.New(texts[2]);
                    BytesPack.Unpack(ref clip, Convert.FromBase64String(texts[3]));
                    clip.EndFrame = mouseFrame + clip.FrameCount - 1;
                    clip.BeginFrame = mouseFrame;
                    clip.Track = Track;
                    ArrayFLibUtility.Add(ref Track.Clips, clip);
                    ClipAreaPanel.RefreshClips();
                    Stage.Tips.Show($"粘贴片段[{texts[1]}]");
                });
            }
            menu.AddItem(new GUIContent($"删除轨道[{Track.Name}]"), false, () =>
            {
                RemoveFromHierarchy();
                ArrayFLibUtility.Remove(ref ParentPanel.Runtime.Tracks, Track);
                Utility.RemoveUnityObjectStoreRef(Track, Stage.Runtime.ExternalReferences);
            });
        }
    }
}
