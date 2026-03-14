// ==================== qcbf@qq.com | 2025-07-01 ====================

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class RightMenuPanel : ImmediateModeElement
    {
        public readonly StagePanel Stage;
        public GUIStyle FontStyle;

        public RightMenuPanel(StagePanel stage)
        {
            Stage = stage;
            style.flexGrow = 1;
            style.paddingLeft = StagePanel.ClipStartSpacer;
            if (stage.IsAllowPlay)
                this.RegisterDragSelf(evt => SetCurrentFrame(this.WorldToLocal(evt.position).x), onDown: (_, evt) => SetCurrentFrame(evt.localPosition.x));
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetCurrentFrame(float x)
        {
            Stage.CurrentFrame.Value = Mathf.Max(0, Mathf.RoundToInt((x - StagePanel.ClipStartSpacer) / Stage.FrameGraphicInterval));
            MarkDirtyRepaint();
        }


        protected override void ImmediateRepaint()
        {
            FontStyle ??= new GUIStyle("MeTimeLabel") { fontSize = 12, alignment = TextAnchor.LowerLeft };
            var rect = contentRect;
            var heightHalf = rect.height * 0.5f;
            var visibleFrameCount = rect.width / Stage.FrameGraphicInterval;
            var bigFrameInterval = Mathf.CeilToInt(30 / Stage.FrameGraphicInterval);
            var endFrame = Stage.EndFrame;
            for (var i = 0; i < visibleFrameCount; i++)
            {
                var x = i * Stage.FrameGraphicInterval;
                var height = rect.height;
                if (i % bigFrameInterval == 0)
                {
                    Handles.Label(new Vector3(x + StagePanel.ClipStartSpacer + 2, height * 0.8f), i.ToString(), FontStyle);
                }
                else
                {
                    height *= 0.3f;
                }
                Graphics.DrawTexture(new Rect(rect.x + x, rect.y + (rect.height - height), 1f, height), endFrame >= i ? Texture2D.whiteTexture : Texture2D.grayTexture);
            }
        }
    }
}
