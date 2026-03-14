//==================={By Qcbf|qcbf@qq.com|10/10/2022 10:18:01 PM}===================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FLib.Unity
{
    public class SpriteAnimator : MonoBehaviour
    {
        public Action PlayFinishEvent;
        public SpriteRenderer Renderer;
        public AnimClip[] Clips;
        public float FrameRate = 15;
        public float Speed = 1;
        public int PlayIndex;
        public byte IsPauseCount;
        public bool IsEnableReset;
        public Dictionary<string, int> ClipNames = new();
        public RuntimeData Runtime;

        public string PlayName => Clips[PlayIndex].Name;

        [Serializable]
        public class AnimClip
        {
            public string Name;
            public bool IsLoop;
            public float Speed = 1;
            public FrameData[] Frames;
        }

        public struct RuntimeData
        {
            public float ElapsedTime;
            public int ClipIndex;
            public float Interval;
            public int FrameIndex;
        }

        [Serializable]
        public struct FrameData
        {
            public Sprite Image;
            public AudioClip Sound;
            public float ExtraTime;
        }

        private void Awake()
        {
            if (IsEnableReset) return;
            RefreshClips();
            UpdateSprite(false);
        }

        private void OnEnable()
        {
            if (!IsEnableReset) return;
            Runtime = default;
            RefreshClips();
            UpdateSprite(false);
        }

        private void RefreshClips()
        {
            ClipNames.Clear();
            for (var i = 0; i < Clips.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) ClipNames.Add(Clips[i].Name, i);
                else
#endif
                if (Clips[i].Name.Length > 0)
                    ClipNames.Add(Clips[i].Name, i);
            }
            Runtime.Interval = 1f / FrameRate;
        }

        private void Update()
        {
            if (IsPauseCount > 0) return;
            var isChanged = Runtime.ClipIndex != PlayIndex;
            if (isChanged)
            {
                Runtime.ClipIndex = PlayIndex;
                Runtime.FrameIndex = 0;
                Runtime.Interval = 1 / FrameRate;
                UpdateSprite();
            }
            else
            {
                var clip = Clips[Runtime.ClipIndex];
                var triggerTime = clip.Frames[Runtime.FrameIndex].ExtraTime + Runtime.Interval / Speed / clip.Speed;
                var num = (int)(Runtime.ElapsedTime / triggerTime);
                if (num >= 1)
                {
                    Runtime.ElapsedTime -= num * triggerTime;
                    if (++Runtime.FrameIndex >= clip.Frames.Length)
                    {
                        if (Clips[Runtime.ClipIndex].IsLoop
#if UNITY_EDITOR
                            || !Application.isPlaying
#endif
                           )
                        {
                            Runtime.FrameIndex = 0;
                        }
                        else
                        {
                            --Runtime.FrameIndex;
                        }
                    }
                    PlayFinishEvent?.Invoke();
                    UpdateSprite();
                }
            }
            Runtime.ElapsedTime += Time.deltaTime;
        }

        private void UpdateSprite(bool isPlaySound = true)
        {
            ref var frame = ref Clips[Runtime.ClipIndex].Frames[Runtime.FrameIndex];
            if (Renderer.sprite == frame.Image) return;
            Renderer.sprite = frame.Image;
            if (isPlaySound && frame.Sound != null)
            {
                Audio.PlayShort(frame.Sound);
            }
        }


        public void Play(string animName, bool isReplay = true)
        {
            if (!ClipNames.TryGetValue(animName, out var index))
            {
                Log.Error?.Write(gameObject + " not found anim name: " + animName);
                return;
            }
            Play(index, isReplay);
        }

        public void Play(int index, bool isReplay = false)
        {
            if (!isReplay && PlayIndex == index)
            {
                return;
            }
            //Log.Info?.Write($"{gameObject} Play Anim: {Clips[index].Name}");
            PlayIndex = index;
            Runtime.ClipIndex = PlayIndex;
            Runtime.FrameIndex = 0;
            Runtime.ElapsedTime = 0;
            UpdateSprite();
        }


        public void AddClips(string animName, FrameData[] sprites, bool isLoop)
        {
            ArrayFLibUtility.Add(ref Clips, null);
            ref var clip = ref Clips[^1];
            clip.Frames = sprites;
            clip.IsLoop = isLoop;
            clip.Name = animName;
            RefreshClips();
        }


#if UNITY_EDITOR

        [ContextMenu(nameof(AutoAddClips))]
        public void AutoAddClips()
        {
            if (Renderer == null)
            {
                Renderer = GetComponent<SpriteRenderer>();
            }
            if (Clips?.Length > 0)
            {
                RefreshClips();
            }
            var clipAssets = new Dictionary<string, List<string>>();
            var files = Directory.GetFiles(Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(Renderer.sprite))!).ToList();
            files.Sort((a, b) =>
            {
                var v = a.Length - b.Length;
                return v != 0 ? v : string.CompareOrdinal(a, b);
            });
            foreach (var path in files)
            {
                if (path.EndsWith(".meta")) continue;
                var fileName = Path.GetFileNameWithoutExtension(path);
                var fileNameSplit = fileName.StartsWith("skeleton-") ? fileName["skeleton-".Length..].Split('_', 2) : fileName.Split('@', 2); //spine
                if (fileNameSplit.Length != 2) continue;
                if (!clipAssets.TryGetValue(fileNameSplit[0], out var list))
                {
                    clipAssets.Add(fileNameSplit[0], list = new List<string>());
                }
                list.Add(path);
            }

            foreach (var item in clipAssets)
            {
                if (ClipNames.TryGetValue(item.Key, out var clipIndex))
                {
                    SetClips(Clips![clipIndex], item.Value);
                }
                else
                {
                    var clip = new AnimClip { Name = item.Key };
                    ArrayFLibUtility.Add(ref Clips, clip);
                    SetClips(clip, item.Value);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            return;

            static void SetClips(AnimClip clip, in List<string> assets)
            {
                Log.Info?.Write($"set clips: {clip.Name} {assets.Count}\n{string.Join('\n', assets)}");
                if (clip.Frames == null)
                {
                    clip.Frames = new FrameData[assets.Count];
                }
                else if (clip.Frames.Length != assets.Count)
                {
                    Array.Resize(ref clip.Frames, assets.Count);
                }

                for (var i = 0; i < assets.Count; i++)
                {
                    clip.Frames[i].Image = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assets[i]);
                }
            }
        }


        private static CancellationTokenSource EditorPlayRuning;

        [ContextMenu("Stop")]
        public void EditorStop()
        {
            if (EditorPlayRuning == null) return;
            EditorPlayRuning.Cancel();
            EditorPlayRuning.Dispose();
            EditorPlayRuning = null;
            Runtime.FrameIndex = 0;
            UpdateSprite();
        }

        [ContextMenu("Play")]
        public void EditorPlay()
        {
            Runtime.ClipIndex = -1;
            EditorStop();
            EditorPlayRuning = new CancellationTokenSource();
            RefreshClips();
            UniTask.Void(async cancelToken =>
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    await UniTask.Delay(100, cancellationToken: cancelToken);
                    if (this == null)
                    {
                        EditorPlayRuning!.Cancel();
                        EditorPlayRuning.Dispose();
                        EditorPlayRuning = null;
                    }
                    else if (!cancelToken.IsCancellationRequested)
                    {
                        Update();
                    }
                }
            }, EditorPlayRuning.Token);
        }
#endif
    }
}
