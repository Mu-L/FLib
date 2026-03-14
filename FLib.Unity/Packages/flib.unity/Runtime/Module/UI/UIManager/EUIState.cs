using System;

namespace FLib.Unity
{
    [Flags]
    public enum EUIState : byte
    {
        None,
        Loading = 1,
        Activating = 1 << 1,
        Destroyed = 1 << 3,
    }
}
