//==================={By Qcbf|qcbf@qq.com|8/22/2022 10:44:40 AM}===================

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public class AudioPlayer : MonoBehaviour
    {
        public AudioClip Clip;
        public string Path;
        public bool IsAutoPlay = true;
        public EType Type;
        public float Delay;

        private AudioPlaying _playing;

        public enum EType : byte
        {
            None,
            ShortFx,
            ShortFxAndIgnoreSameLimit,
            LoopBgm,
        }

        protected virtual void OnEnable()
        {
            if (IsAutoPlay)
                Play();
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (!gameObject.scene.isLoaded) return;
#endif
            if (_playing != null)
                _playing.Stop();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Play()
        {
            if (Delay > 0)
                DelayPlay(Delay).Forget();
            else
                ImmediatePlay();
        }

        /// <summary>
        /// 
        /// </summary>
        private async UniTaskVoid DelayPlay(float delay)
        {
            await UniTask.Delay((int)(delay * 1000));
            if (this != null && isActiveAndEnabled)
                ImmediatePlay();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ImmediatePlay()
        {
            if (Type is EType.ShortFx or EType.ShortFxAndIgnoreSameLimit)
            {
                Audio.PlayShort(Clip, Type == EType.ShortFxAndIgnoreSameLimit);
            }
            else if (_playing == null)
            {
                _playing = Clip != null ? Audio.Play(Clip, transform.position) : Audio.Play(Path, transform.position);
                _playing.Source.loop = Type == EType.LoopBgm;
            }
            else
            {
                _playing.Play();
            }
        }
    }
}
