// ==================== qcbf@qq.com | 2025-07-01 ====================

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class ClipAreaPanel : ImmediateModeElement
    {
        public TrackItemPanel ParentPanel;
        public StagePanel Stage => ParentPanel.Stage;

        public ClipAreaPanel(TrackItemPanel parentPanel)
        {
            ParentPanel = parentPanel;
            style.flexGrow = 1;
            style.marginLeft = StagePanel.ClipStartSpacer;
            style.flexDirection = FlexDirection.Row;
            RefreshClips();
        }

        protected override void ImmediateRepaint()
        {
            var rect = contentRect;
            var heightHalf = rect.height * 0.5f;
            var texRect = new Rect(0, 0, 1, 1);
            var visibleFrameCount = rect.width / Stage.FrameGraphicInterval;
            for (var i = 0; i < visibleFrameCount; i++)
            {
                var x = contentRect.x + i * Stage.FrameGraphicInterval;
                Graphics.DrawTexture(new Rect(x, rect.y, 1f, rect.height), Texture2D.grayTexture);
                Graphics.DrawTexture(new Rect(x - 1f, rect.y + heightHalf - 4, 3f, 8f), Texture2D.grayTexture);
            }
        }

        public void RefreshClips()
        {
            Clear();
            foreach (var clip in ParentPanel.Track.Clips)
            {
                var type = FLibCustomEditorAttribute.CustomEditors.GetValueOrDefault(clip.GetType(), typeof(ClipItemPanel));
                Add((ClipItemPanel)TypeAssistant.New(type, this, clip));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int GetFrameFromX(StagePanel stage, float x)
        {
            return (int)(x / stage.FrameGraphicInterval);
        }

        /// <summary>
        /// 
        /// </summary>
        public ClipItemPanel GetClipItemByFrame(int frame)
        {
            foreach (var item in this.Query<ClipItemPanel>().Build())
            {
                if (item.Clip.CheckFrame(frame))
                    return item;
            }
            return null;
        }
    }
}
