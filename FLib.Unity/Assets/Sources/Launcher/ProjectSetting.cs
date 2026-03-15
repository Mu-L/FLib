// ==================== qcbf@qq.com | 2025-09-09 ====================

using FLib;
using FLib.Unity;

namespace Modules
{
    [Comment("项目启动设置")]
    public class ProjectSetting : FLibUnityLaunchSetting
    {
        public static ProjectSetting Inst;
        public string Address;
        public int Port;
        public EPlatform platform;

        public enum EPlatform
        {
            None,
        }
        
        public override void Active()
        {
            base.Active();
            Inst = this;
        }
    }
}
