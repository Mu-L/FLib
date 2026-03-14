// ==================== qcbf@qq.com | 2025-08-08 ====================

using System;

namespace FLib.Unity
{
    public interface IUIAnimatable
    {
        void PlayForward(bool withActiveGameObject);
        void PlayBackward(bool trueDisableOrFalseDestroy);
    }
}
