//==================={By Qcbf|qcbf@qq.com|1/13/2023 10:56:38 AM}===================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLib.Unity
{
    [AddComponentMenu("/")]
    public class AudioPlaying : MonoBehaviour, IAssetLoadReferenceable
    {
        public static List<AudioPlaying> BackgroundMusics = new();

        public Audio.Template Template;
        public AudioSource Source;
        public EState State;
        public bool IsAssetUsed => Template != null;


        public enum EState
        {
            None,
            Loading,
            Playing,
            Paused,
        }

        // public void OnObjectPoolDeactivate()
        // {
        //     State = default;
        //     Source.Stop();
        //     Source.clip = null;
        //     Template = null;
        //     gameObject.SetActive(false);
        // }

        // public void OnObjectPoolActivate()
        // {
        //     gameObject.SetActive(true);
        // }


        public void LoadAndPlay(in AssetLoaderPath path)
        {
            Source.loop = Template.Source.loop;
            State = EState.Loading;
            AssetLoader.Load(new AssetLoadReference(this), path, OnLoadFinished);
        }

        private void OnLoadFinished(AssetLoading.Result obj)
        {
            if (Template == null) return;
            Source.clip = (AudioClip)obj.Loaded.MainAsset;
            Play();
        }


        public void Play()
        {
            if (State == EState.Playing) return;
            State = EState.Playing;
            Source.Play();
            if (Template.FadeIn > 0)
            {
                Source.volume = 0;
                StopAllCoroutines();
                StartCoroutine(Fade(Template.FadeIn, Template.Volume, null));
            }
            if (Template.IsBgm) PushBgm();
        }


        public void Pause()
        {
            if (State == EState.Paused) return;
            State = EState.Paused;
            StopAllCoroutines();
            StartCoroutine(Fade(-Template.FadeOut, 0, Source.Stop));
            if (Template.IsBgm) PopBgm();
        }


        public void Stop()
        {
            if (State == EState.Playing && Template.FadeOut > 0)
            {
                StopAllCoroutines();
                if (Template.IsBgm) PopBgm();
                StartCoroutine(Fade(-Template.FadeOut, 0, () => Destroy(gameObject)));
            }
            else
            {
                if (Template.IsBgm) PopBgm();
                Destroy(gameObject);
                // Template.Pool.Release(this);
            }
        }


        private void PushBgm()
        {
            if (BackgroundMusics.Count > 0)
                BackgroundMusics[^1].Pause();
            BackgroundMusics.Add(this);
        }

        private void PopBgm()
        {
            if (BackgroundMusics[^1] == this)
            {
                BackgroundMusics.RemoveAt(BackgroundMusics.Count - 1);
                if (BackgroundMusics.Count > 0)
                    BackgroundMusics[^1].Play();
            }
            else
            {
                BackgroundMusics.Remove(this);
            }
        }


        private IEnumerator Fade(float add, float to, Action continueWith)
        {
            while ((add > 0 && Source.volume < to) || (add < 0 && Source.volume > to))
            {
                Source.volume += Time.deltaTime * (1 / add);
                yield return 0;
            }
            Source.volume = to;
            continueWith?.Invoke();
        }
    }
}
