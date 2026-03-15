// ==================== qcbf@qq.com | 2025-08-06 ====================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using FLib;
using FLib.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    public class HighlightAnimation : MonoBehaviour
    {
        public static HighlightAnimation Inst;
        public ValueLinkedList<PlayingData> Playings;
        public Dictionary<int, int> PlayingIds = new();

        [StructLayout(LayoutKind.Auto)]
        public struct PlayingData
        {
            public static PlayingData Default = new() { Duration = 0.4f, Scale = 2f, Color = new Color(0.4575f, 1f, 0.858f) };
            public int Id;
            public float Time;
            public float Duration;
            public float Scale;
            public float SourceScale;
            public Color Color;
            public Color SourceColor;
            public Graphic ColorElement;
            public Transform ScaleElement;

            public void Set(float scale, in Color color)
            {
                Scale = scale;
                Color = color;
            }
        }

        private void Awake() => Inst = this;
        private void OnDestroy() => Inst = null;

        private void Update()
        {
            var enumerator = Playings.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ref var data = ref enumerator.Current;
                if (data.ScaleElement == null && data.ColorElement == null)
                {
                    data.Time = 1;
                }
                else
                {
                    data.Time += Time.smoothDeltaTime;
                    if (data.Time > 1)
                        data.Time = 1;
                    var t = EaseManager.Evaluate(Ease.InOutBack, null, data.Time, data.Duration, DOTween.defaultEaseOvershootOrAmplitude, DOTween.defaultEasePeriod);
                    if (data.ColorElement != null)
                        data.ColorElement.color = Color.Lerp(data.Color, data.SourceColor, t);
                    if (data.ScaleElement != null)
                    {
                        var scale = Mathf.Lerp(data.Scale, data.SourceScale, t);
                        data.ScaleElement.localScale = new Vector3(scale, scale, scale);
                    }
                }
                if (data.Time >= 1)
                {
                    PlayingIds.Remove(data.Id);
                    Playings.RemoveAt(enumerator.Index);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static ref PlayingData Play(Graphic colorElement, Transform scaleElement)
        {
            if (Inst == null)
                Inst = UIRoot.Inst.gameObject.AddComponent<HighlightAnimation>();
            var id = HashCode.Combine(colorElement?.GetInstanceID(), scaleElement?.GetInstanceID());
            if (Inst.PlayingIds.TryGetValue(id, out var index))
            {
                ref var data = ref Inst.Playings[index];
                data.Time = 0;
                return ref data;
            }
            else
            {
                index = Inst.Playings.Add(PlayingData.Default);
                Inst.PlayingIds.Add(id, index);
                ref var data = ref Inst.Playings[index];
                data.Id = id;
                if (scaleElement != null)
                    data.SourceScale = scaleElement.localScale.x;
                if (colorElement != null)
                    data.SourceColor = colorElement.color;
                data.ColorElement = colorElement;
                data.ScaleElement = scaleElement;
                return ref data;
            }
        }
    }
}
