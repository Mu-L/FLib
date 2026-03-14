// ==================== qcbf@qq.com | 2025-07-01 ====================

using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class CurrentFramePanel : ImmediateModeElement
    {
        public readonly StagePanel Stage;

        public CurrentFramePanel(StagePanel stage)
        {
            Stage = stage;
            style.position = Position.Absolute;
            style.width = style.height = new Length(100, LengthUnit.Percent);
            pickingMode = PickingMode.Ignore;
        }

        protected override void ImmediateRepaint()
        {
            var rect = contentRect;
            var x = Stage.TrackListWidth + StagePanel.ClipStartSpacer + Stage.CurrentFrame * Stage.FrameGraphicInterval;
            Graphics.DrawTexture(new Rect(x, rect.y, 1, rect.height), Texture2D.whiteTexture);
        }
    }
}
