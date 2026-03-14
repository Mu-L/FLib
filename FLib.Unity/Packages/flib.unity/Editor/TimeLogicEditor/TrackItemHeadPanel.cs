// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class TrackItemHeadPanel : VisualElement, IContextMenuProcessable
    {
        private TrackItemPanel _item;

        public TrackItemHeadPanel(TrackItemPanel item)
        {
            _item = item;
            var stage = item.Stage;
            var track = item.Track;
            var head = new VisualElement() { style = { width = stage.TrackListWidth.Value, flexDirection = FlexDirection.Row } };
            Add(head);
            new Toggle().BindDataWithUI(v => track.IsDisable = !v, () => !track.IsDisable).AddToUI(head);
            new Label(track.Name)
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        fontSize = 12, unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 1, overflow = Overflow.Hidden,
                        textOverflow = TextOverflow.Ellipsis, unityTextOverflowPosition = TextOverflowPosition.Middle,
                        maxWidth = stage.TrackListWidth.Value - 22,
                    }
                }
                .BindDataToUI(v => v.text = track.Name).AddToUI(head);
            head.Add(new VisualElement() { style = { width = 2, backgroundColor = (EditorGUIUtility.isProSkin ? Color.black : Color.white) * 0.9f } });
        }

        public void ContextMenuProcess(MouseUpEvent evt, GenericMenu menu)
        {
            var tracks = _item.Stage.Runtime.Tracks;
            var index = Array.IndexOf(tracks, _item.Track);
            if (index > 0)
            {
                menu.AddItem(new GUIContent("上移动"), false, () =>
                {
                    ArrayFLibUtility.ChangeIndex(tracks, index, index - 1);
                    _item.ParentPanel.RefreshTracks();
                });
            }
            if (index < tracks.Length - 1)
            {
                menu.AddItem(new GUIContent("下移动"), false, () =>
                {
                    ArrayFLibUtility.ChangeIndex(tracks, index, index + 1);
                    _item.ParentPanel.RefreshTracks();
                });
            }
        }
    }
}
