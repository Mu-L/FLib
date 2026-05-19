// ==================== qcbf@qq.com | 2025-07-01 ====================

namespace FLib.WorldCores.TimeLogics
{
    [Comment("[编辑器]预览轨道")]
    public class TimeLogicEditorOnlyTrack : TimeLogicTrack
    {
        public override void Z_BytesPackRead(int key, ref BytesReader reader)
        {
#if UNITY_EDITOR
            base.Z_BytesPackRead(key, ref reader);
#else
            reader.Position = -reader.Position;
#endif
        }
    }
}
