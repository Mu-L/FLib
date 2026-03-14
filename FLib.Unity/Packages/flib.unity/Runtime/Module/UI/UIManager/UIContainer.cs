using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FLib.Unity
{
    public class UIContainer
    {
        public event Action<bool, UIContext> OnOpenUIEvent;
        public RectTransform Root;
        public bool UseSafeArea;
        public Dictionary<Type, UIContext> OpenedUIs = new();
        public List<UIContext> History;
        public UIContainer[] HiddenContainers;

        /// <summary>
        /// 
        /// </summary>
        public UIContext Get<T>(ELogLevel logLevel = ELogLevel.Fatal) where T : UIBase => Get(typeof(T), logLevel);

        /// <summary>
        /// 
        /// </summary>
        public T GetUI<T>(ELogLevel logLevel = ELogLevel.Fatal) where T : UIBase => (T)Get(typeof(T), logLevel).LoadedUI;

        /// <summary>
        /// 
        /// </summary>
        public UIContext Get(Type uiType, ELogLevel logLevel = ELogLevel.Fatal)
        {
            if (!OpenedUIs.TryGetValue(uiType, out var ui))
                Log.Get(logLevel)?.Write($"not found ui: {uiType}");
            return ui;
        }

        /// <summary>
        /// 
        /// </summary>
        public UIContext Open<T>() where T : UIBase => Open(typeof(T));

        /// <summary>
        /// 
        /// </summary>
        public UIContext Open(Type uiType)
        {
            using var key = InputBlocker.Open("Open UI");
            if (OpenedUIs.TryGetValue(uiType, out var ctx))
            {
                ctx = ctx.Reopen();
            }
            else
            {
                if (HiddenContainers != null && OpenedUIs.Count == 0)
                {
                    foreach (var item in HiddenContainers)
                        item.Root.gameObject.SetActive(false);
                }

                var meta = UIMeta.Get(uiType);
                ctx = (UIContext)TypeAssistant.New(meta.ContextType);
                OpenedUIs.Add(uiType, ctx.Initialize(meta, uiType, this));
                OnOpenUIEvent?.Invoke(true, ctx);
            }
            return ctx;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close(Type uiType)
        {
            if (OpenedUIs.TryGetValue(uiType, out var ui)) Close(ui);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close(UIContext ctx)
        {
            OpenedUIs.Remove(ctx.UIType, out var removedCtx);
            if (OpenedUIs.Count == 0 && HiddenContainers != null)
            {
                foreach (var item in HiddenContainers)
                    item.Root.gameObject.SetActive(true);
            }
            Log.Assert(removedCtx == ctx);
            OnOpenUIEvent?.Invoke(false, ctx);
            ctx.Destroy();
        }

        /// <summary>
        /// 
        /// </summary>
        public void CloseAll()
        {
            if (HiddenContainers != null)
            {
                foreach (var item in HiddenContainers)
                    item.Root.gameObject.SetActive(true);
            }
            History?.Clear();
            foreach (var item in OpenedUIs)
                item.Value.Destroy();
            OpenedUIs.Clear();
            OnOpenUIEvent?.Invoke(false, null);
        }

        public void RefreshSafeArea()
        {
            if (!UseSafeArea) return;
            switch (Screen.orientation)
            {
                case ScreenOrientation.PortraitUpsideDown:
                    Root.offsetMax = new Vector2(0, 0);
                    Root.offsetMin = new Vector2(0, UIRoot.SafeArea.y);
                    break;
                case ScreenOrientation.Portrait:
                    Root.offsetMax = new Vector2(0, -UIRoot.SafeArea.y);
                    Root.offsetMin = new Vector2(0, 0);
                    break;
                case ScreenOrientation.LandscapeLeft:
                    Root.offsetMax = new Vector2(0, 0);
                    Root.offsetMin = new Vector2(UIRoot.SafeArea.x, 0);
                    break;
                case ScreenOrientation.LandscapeRight:
                    Root.offsetMax = new Vector2(-UIRoot.SafeArea.x, 0);
                    Root.offsetMin = new Vector2(0, 0);
                    break;
            }
        }
    }
}
