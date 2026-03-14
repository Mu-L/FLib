// ==================== qcbf@qq.com | 2025-07-01 ====================

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FLib.Unity
{
    public interface IUnityLaunchable
    {
        UniTask Prelaunch(FLibUnityLauncher launcher);
        UniTask Launch(FLibUnityLauncher launcher);
    }
}
