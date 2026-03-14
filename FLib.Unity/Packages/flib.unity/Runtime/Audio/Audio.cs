//==================={By Qcbf|qcbf@qq.com|8/21/2022 3:01:49 PM}===================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FLib;
using JetBrains.Annotations;
using UnityEngine;
using static FLib.Unity.Audio;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    public static class Audio
    {
        public static Action<string, float> SetAudioSetting = PlayerPrefs.SetFloat;
        public static Func<string, float, float> GetAudioSetting = PlayerPrefs.GetFloat;
        public static readonly Dictionary<string, Template> Templates = new();
        private static readonly Dictionary<string, List<float>> mPlayShortAudioNames = new();

        [Serializable]
        public class Template
        {
            public AudioSource Source;
            public float FadeIn;
            public float FadeOut;
            public bool IsBgm;
            public int ShortAudioMaxSameCount = 3;

            [HideInInspector]
            public Transform Root;

            public float Volume
            {
                get => Source.volume;
                set
                {
                    var v = Mathf.Clamp01(value);
                    SetAudioSetting(nameof(Volume) + Source.name, Source.volume = v);
                }
            }

            public Template Initialize()
            {
                var name = Source.name;
                Root = new GameObject(name).transform;
                Root.SetParent(Source.transform.root);
                Source.playOnAwake = false;
                Volume = GetAudioSetting(nameof(Volume) + name, Source.volume);
                return this;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static async UniTask PlayShort(string path, bool isMoveToConst = false, AssetLoadReference reference = default, bool isIgnoreSameLimit = false)
        {
            if (string.IsNullOrEmpty(path))
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(System.IO.Path.Combine("Assets", AssetLoader.GAME_RES_NAME, path));
                PlayShort(clip, isIgnoreSameLimit);
                return;
            }
#endif
            var isValid = reference.IsValid;
            var loaded = await AssetLoader.Load(path);
            if (isMoveToConst && !AssetLoader.ConstAssetLoadeds.ContainsKey(loaded.Path))
                AssetLoader.MoveToConstAsset(loaded);
            if (isValid == reference.IsValid)
                PlayShort(loaded.GetMainAsset<AudioClip>(), isIgnoreSameLimit);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PlayShort(AudioClip clip, bool isIgnoreSameLimit = false)
        {
            var t = Time.time;
            var template = GetTemplate(clip.name);
            if (!isIgnoreSameLimit)
            {
                if (mPlayShortAudioNames.TryGetValue(clip.name, out var playings))
                {
                    if (playings[^1] == t)
                        return;
                    if (playings.Count >= template.ShortAudioMaxSameCount)
                    {
                        var len = clip.length;
                        for (var i = playings.Count - 1; i >= 0; i--)
                        {
                            if (t - playings[i] > len)
                                playings.RemoveAt(i);
                        }
                    }
                    if (playings.Count >= template.ShortAudioMaxSameCount)
                        return;
                }
                else
                {
                    mPlayShortAudioNames.Add(clip.name, playings = new List<float>());
                }
                playings.Add(t);
            }
            template.Source.PlayOneShot(clip);
        }

        /// <summary>
        /// 
        /// </summary>
        public static AudioPlaying Play(AudioClip clip, Vector3 position)
        {
            var p = Create(GetTemplate(clip.name), clip, position);
#if UNITY_EDITOR
            p.name = clip.name;
#endif
            p.Play();
            return p;
        }

        /// <summary>
        /// 
        /// </summary>
        public static AudioPlaying Play(string path, Vector3 position)
        {
            var p = Create(GetTemplate(path), null, position);
#if UNITY_EDITOR
            p.name = System.IO.Path.GetFileNameWithoutExtension(path);
#endif
            p.LoadAndPlay(path);
            return p;
        }

        /// <summary>
        /// 
        /// </summary>
        public static AudioPlaying Create(Template template, AudioClip clip, Vector3 position)
        {
            var audioSource = Object.Instantiate(template.Source, template.Root);
            var p = audioSource.gameObject.AddComponent<AudioPlaying>();
            p.Template = template;
            p.Source = audioSource;
            p.Source.clip = clip;
            p.Source.volume = template.Volume;
            p.Source.transform.position = position;
            return p;
        }

        /// <summary>
        /// 
        /// </summary>
        private static string GetTemplateName(string fullname)
        {
            var index = fullname.LastIndexOf('@') + 1;
            if (index <= 0) throw new Exception("audio not found audio template name\n" + fullname);
            return FIO.RemoveExtension(fullname[index..]);
        }

        /// <summary>
        /// 
        /// </summary>
        private static Template GetTemplate(string fullname)
        {
#if UNITY_EDITOR
            if (Templates.TryGetValue("*", out var editorTemplate) && editorTemplate.Source != null)
                return editorTemplate;
            if (Templates.Count == 0 || editorTemplate != null)
            {
                var template = new Template() { Source = new GameObject("[EDITOR AUDIO]").AddComponent<AudioSource>() };
                template.Source.gameObject.hideFlags = HideFlags.HideAndDontSave;
                Templates["*"] = template;
                return template;
            }
#endif
            if (Templates.TryGetValue(GetTemplateName(fullname), out var t))
                return t;
            throw new Exception("not found audio template: " + GetTemplateName(fullname));
        }
    }
}
