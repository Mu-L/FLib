//==================={By Qcbf|qcbf@qq.com|8/22/2022 10:37:19 AM}===================

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    [ExecuteAlways]
    public class AudioInitializer : MonoBehaviour
    {
        [Header("音频模版")]
        public Audio.Template[] TemplateSources = Array.Empty<Audio.Template>();

        private void Awake()
        {
            Audio.Templates.Clear();
            Audio.Templates.EnsureCapacity(TemplateSources.Length);
            foreach (var item in TemplateSources)
            {
                var templateName = item.Source.name;
                if (templateName[0] == '@')
                    templateName = templateName[1..];
                Audio.Templates.Add(templateName, item.Initialize());
            }
        }

        private void OnDestroy()
        {
            Audio.Templates.Clear();
        }

#if UNITY_EDITOR
        [MethodButton("说明")]
        internal static void Tips()
        {
            UnityEditor.EditorUtility.DisplayDialog("音效提示", @"
音频模版指定每一种音频的播放参数,淡如淡出时长
每一个音频文件需要@音频模版结尾
例子:
    配置音频模版名称是ui-sound, 使用这个模版播放的音频文件需要命名为 
        音频1@ui-sound.mp3
        按钮点击音效@ui-sound.mp3
", "close");
        }
#endif
    }
}
