// =================================================={By Qcbf|qcbf@qq.com|2024-09-18}==================================================

using System;

namespace Modules
{
    [Flags]
    public enum EModuleStage : byte
    {
        None,
        Login = 1 << 0,
        Home = 1 << 1,
        Battle = 1 << 2,
        Logined = Home | Battle,
    }
}
