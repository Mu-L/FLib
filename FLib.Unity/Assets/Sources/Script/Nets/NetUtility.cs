// ==================== qcbf@qq.com | 2025-07-29 ====================

using System;
using System.Runtime.CompilerServices;
using Configs;
using Modules.SmallNotifier;

namespace Utilities
{
    public static class NetUtility
    {
        public static void Verify<T>(T code) where T : Enum
        {
            if (Unsafe.SizeOf<T>() switch
                {
                    1 => Unsafe.As<T, byte>(ref code),
                    2 => Unsafe.As<T, short>(ref code),
                    4 => Unsafe.As<T, int>(ref code),
                    8 => Unsafe.As<T, long>(ref code),
                    _ => throw new NotSupportedException(code.ToString())
                } != 0)
                SmallNotifierUI.Open(new SmallNotifierUI.OptionData(Lang.Get(code.ToString())) { IsHighlight = true });
        }
    }
}
