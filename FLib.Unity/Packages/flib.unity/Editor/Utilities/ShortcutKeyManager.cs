//==================={By Qcbf|qcbf@qq.com|6/13/2021 3:38:56 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class ShortcutKeyManager
    {
        private readonly Dictionary<int, Data> mAllKeys = new();

        private struct Data
        {
            public string Name;
            public object Args;
            public bool InputFocusStillProcess;
            public bool SureFocusElement;
            public Action Handler;
            public Action<object> Handler2;
        }


        [Flags]
        public enum EModifier
        {
            None = 0,
            Ctrl = 0x1000,
            Shift = 0x2000,
            Alt = 0x4000,
        }

        /// <summary>
        /// 
        /// </summary>
        public void RegisterKeyEvent(VisualElement el, bool isRootElement = false)
        {
            if (el.panel == null)
                el.RegisterCallbackOnce<AttachToPanelEvent>(evt => Impl(evt.destinationPanel));
            else
                Impl(el.panel);
            return;

            void Impl(IPanel panel)
            {
                var ui = el;
                if (isRootElement)
                {
                    ui = panel.visualTree;
                    el.RegisterCallbackOnce<DetachFromPanelEvent>(_ => ui.UnregisterCallback<KeyDownEvent>(OnKeyEvent, TrickleDown.TrickleDown));
                }
                ui.focusable = true;
                ui.RegisterCallback<KeyDownEvent>(OnKeyEvent, TrickleDown.TrickleDown);
                return;

                void OnKeyEvent(KeyDownEvent e)
                {
                    if (InputKey(panel, e.keyCode, e.ctrlKey, e.shiftKey, e.altKey))
                        e.StopPropagation();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ShortcutKeyManager Register(string name, KeyCode key, EModifier modifier, Action action, object args = null, bool inputFocusStillProcess = false, bool sureFocusElement = false)
        {
            mAllKeys[(int)key | (int)modifier] = new Data { Name = name, Handler = action, Args = args, InputFocusStillProcess = inputFocusStillProcess, SureFocusElement = sureFocusElement };
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public ShortcutKeyManager Register(string name, KeyCode key, EModifier modifier, Action<object> action, object args = null)
        {
            mAllKeys[(int)key | (int)modifier] = new Data { Name = name, Handler2 = action, Args = args };
            return this;
        }


        /// <summary>
        /// 
        /// </summary>
        public ShortcutKeyManager Unregister(KeyCode key, EModifier modifier)
        {
            mAllKeys.Remove((int)key | (int)modifier);
            return this;
        }


        /// <summary>
        /// 
        /// </summary>
        public ShortcutKeyManager Unregister(Action action)
        {
            foreach (var item in mAllKeys)
            {
                if (item.Value.Handler == action)
                {
                    mAllKeys.Remove(item.Key);
                    break;
                }
            }
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public ShortcutKeyManager Unregister(Action<object> action)
        {
            foreach (var item in mAllKeys)
            {
                if (item.Value.Handler2 == action)
                {
                    mAllKeys.Remove(item.Key);
                    break;
                }
            }
            return this;
        }


        /// <summary>
        /// 
        /// </summary>
        public bool InputKey(IPanel panel, KeyCode key, bool isCtrl, bool isShift, bool isAlt)
        {
            var finalKey = (int)key;
            if (isCtrl) finalKey |= (int)EModifier.Ctrl;
            if (isShift) finalKey |= (int)EModifier.Shift;
            if (isAlt) finalKey |= (int)EModifier.Alt;

            if (mAllKeys.TryGetValue(finalKey, out var data) && (data.InputFocusStillProcess || panel?.CheckFocusInInput() != true))
            {
                if (panel != null && data.SureFocusElement)
                    panel.visualTree.Focus();
                if (data.Handler != null)
                    data.Handler.Invoke();
                else
                    data.Handler2?.Invoke(data.Args);
                return true;
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        public string GetTips(string template = "{$name}\t\t\t{$key}\n\n")
        {
            var strbuf = new StringBuilder();
            var regex = new Regex(@"(.*)\{\$name\}(.*)\{\$key\}(.*)", RegexOptions.IgnoreCase);
            foreach (var item in mAllKeys)
            {
                strbuf.Append(regex.Replace(template, "$1" + item.Value.Name + "$2" + GetKeyName(item.Key) + "$3"));
            }
            return strbuf.ToString();
        }

        public string GetKeyName(int key)
        {
            var strbuf = new StringBuilder();
            if ((key & (int)EModifier.Ctrl) != 0) strbuf.Append("ctrl").Append(',');
            if ((key & (int)EModifier.Shift) != 0) strbuf.Append("shift").Append(',');
            if ((key & (int)EModifier.Alt) != 0) strbuf.Append("alt").Append(',');
            if (strbuf.Length > 0 && strbuf[strbuf.Length - 1] == ',') strbuf.Remove(strbuf.Length - 1, 1).Append('+');
            strbuf.Append((KeyCode)(key & 0xfff));
            return strbuf.ToString();
        }
    }
}
