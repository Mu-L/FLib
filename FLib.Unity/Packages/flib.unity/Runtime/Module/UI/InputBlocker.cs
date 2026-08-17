//==================={By Qcbf|qcbf@qq.com|12/26/2021 12:26:13 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace FLib.Unity
{
    public class InputBlocker : MonoBehaviour
    {
        public static InputBlocker Inst;
        public static float Timeout = 20;

        public Action OnTimeoutEvent;
        public UnityEvent OnActive;
        public UnityEvent OnDeactive;

        public GameObject Content;
        public TMP_Text Label;


        public static readonly Dictionary2<int, Blocking> NamedBlockings = new(8);
        public static int AnonymousBlockings;
        private static float _lastActiveTime;
        private static string _defaultLabelText;

        public static bool IsActive => NamedBlockings.Count > 0 || AnonymousBlockings > 0;


        public struct Blocking
        {
            public string DisplayText;
            public int Count;
        }

        public readonly struct BlockingRef : IDisposable
        {
            public readonly int Key;

            public BlockingRef(int key) => Key = key;
            public void Dispose() => Close(Key);
            public static implicit operator int(in BlockingRef v) => v.Key;
            public static implicit operator BlockingRef(in int v) => new(v);
        }

        private void Awake()
        {
            Inst = this;
            if (Label == null)
                Label = gameObject.AddComponent<TextMeshProUGUI>();

            if (Content == null)
                Content = gameObject;

            _defaultLabelText = Label.text;

            Content.SetActive(false);
        }

        private void Update()
        {
            if (_lastActiveTime > 0 && Timeout > 0 && Time.time - _lastActiveTime > Timeout)
            {
                NamedBlockings.Clear();
                AnonymousBlockings = 0;
                Deactive();
                OnTimeoutEvent();
            }
        }


        private static void Active()
        {
            if (Inst != null && Inst.Content != null)
            {
                Inst.Content.SetActive(true);
                Inst.OnActive?.Invoke();
            }
        }

        private static void Deactive()
        {
            if (Inst == null) return;
            _lastActiveTime = 0;
            var label = _defaultLabelText;
            if (NamedBlockings.Count > 0)
            {
                label += " " + NamedBlockings.Last().Value.DisplayText;
            }

            Inst.Label.text = label;
            Inst.Content.SetActive(false);
            Inst.OnDeactive?.Invoke();
        }

        public static void Open()
        {
            _lastActiveTime = Time.time;
            if (++AnonymousBlockings == 1 && NamedBlockings.Count == 0)
                Active();
        }

        public static void Close()
        {
            if (--AnonymousBlockings == 0 && NamedBlockings.Count == 0)
                Deactive();
        }

        public static BlockingRef Open(string key, string text = null) => Open(StringFLibUtility.ShortStringToHash(key), text == null ? key : $"{key} {text}");

        public static BlockingRef Open(int key, string text = null)
        {
            _lastActiveTime = Time.time;
            if (!IsActive)
                Active();

            Inst.Label.text = text;
            ref var blocking = ref NamedBlockings.GetValueOrAdd(key);
            blocking.DisplayText = text;
            ++blocking.Count;
            return new BlockingRef(key);
        }


        public static void Close(string key, bool isForce = false) => Close(StringFLibUtility.ShortStringToHash(key), isForce);

        public static void Close(int key, bool isForce = false)
        {
            var index = NamedBlockings.GetEntryIndex(key);
            if (index < 0)
                return;
            ref var blocking = ref NamedBlockings.GetEntryValue(index);
            if (isForce || --blocking.Count <= 0)
            {
                NamedBlockings.Remove(key);
                if (AnonymousBlockings == 0 && NamedBlockings.Count == 0)
                {
                    Deactive();
                }
                else if (Inst.Label.text == blocking.DisplayText)
                {
                    Inst.Label.text = NamedBlockings.FirstOrDefault().Value.DisplayText ?? _defaultLabelText;
                }
            }
        }

        public static void CloseAll()
        {
            if (!IsActive) return;
            NamedBlockings.Clear();
            AnonymousBlockings = 0;
            Deactive();
        }
    }
}
