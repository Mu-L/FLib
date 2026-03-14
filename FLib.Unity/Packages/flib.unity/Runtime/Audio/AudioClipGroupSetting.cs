// =================================================={By Qcbf|qcbf@qq.com|2024-10-30}==================================================

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FLib.Unity
{
    public class AudioClipGroupSetting : ScriptableObject
    {
        public AudioClipGroup Group;
        public UniTask<AudioClip> GetClip() => Group.GetClip();
    }
}
