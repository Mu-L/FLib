// ==================== qcbf@qq.com | 2025-07-01 ====================

namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public class Config : TaskBase
    {
        public override void Execute(Context ctx)
        {
            ConfigToolEditor.BuildConfig();
        }
    }
}
