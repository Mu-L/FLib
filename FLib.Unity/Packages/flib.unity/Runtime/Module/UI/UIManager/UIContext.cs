using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    /// <summary>
    /// 
    /// </summary>
    public class UIContext
    {
        protected internal UniTask LoadingTask;
        public UIMeta Meta { get; private set; }
        public UIContainer Container { get; private set; }
        public Type UIType { get; private set; }
        public EUIState States { get; private set; }
        public UIBase LoadedUI { get; private set; }
        [NonSerialized] public IUIAnimatable UIAnim;

        /// <summary>
        /// 
        /// </summary>
        public virtual UIContext Initialize(UIMeta meta, Type uiType, UIContainer container)
        {
            Meta = meta;
            Container = container;
            UIType = uiType;
            LoadingTask = Load();
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnSwitchScaler()
        {
            if (LoadedUI != null && !string.IsNullOrEmpty(UIRoot.Inst.Secondary.PrefabSuffix) && AssetLoader.ExistsAsset(Meta.SecondaryAssetPath))
                Load().Forget();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual string GetAssetPath()
        {
            return Meta.GetCurrentAssetPath();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual UIContext Reopen()
        {
            SetActive(true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public async UniTask<UIBase> UI()
        {
            if (LoadedUI != null)
                return LoadedUI;
            await LoadingTask;
            return LoadedUI;
        }

        /// <summary>
        /// 
        /// </summary>
        protected internal virtual async UniTask Load()
        {
            Log.Assert(!CheckState(EUIState.Loading))?.Write("loading");
            Log.Debug?.Write(UIType.Name, "load ui");
            AddState(EUIState.Loading);
            try
            {
                using var _ = InputBlocker.Open("Open UI");
                var loaded = await AssetLoader.Load(GetAssetPath(), isAlwaysNextFrame: true);
                RemoveState(EUIState.Loading);
                Log.Debug?.Write($"{UIType.Name} {Json5.SerializeToLog(this)}", "load ui complete");
#if UNITY_EDITOR
                if (UIRoot.Inst == null)
                    return;
#endif
                var isReload = false;
                if (LoadedUI != null)
                {
                    isReload = true;
                    Object.Destroy(LoadedUI.gameObject);
                }

                if (CheckState(EUIState.Destroyed) || loaded.MainAsset == null)
                {
                    Container.OpenedUIs.Remove(UIType);
                    return;
                }

                var src = (GameObject)loaded.MainAsset;
                var srcUI = src.GetComponent<UIBase>();
                if (srcUI == null)
                    throw new Exception($"not found ui script\n{loaded.Path}");
                LoadedUI = Object.Instantiate(srcUI, Container.Root, false);
                loaded.References.Add(LoadedUI);
                UIAnim = LoadedUI.GetComponent<IUIAnimatable>();
                LoadedUI.SelfContext = this;
                LoadedUI.InitializeUI();
                if (!isReload)
                    SetActive(true);
            }
            catch (Exception e)
            {
                Log.Error?.Write($"{Meta.AssetPath}\n{e}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected internal virtual void SetActive(bool v)
        {
            if (v == CheckState(EUIState.Activating | EUIState.Destroyed | EUIState.Loading))
                return;
            if (v)
            {
                AddState(EUIState.Activating);
                if (LoadedUI != null && Container.History != null)
                {
                    var currentActiveUI = Container.History.ElementAtOrDefault(Container.History.Count - 1);
                    Container.History.Add(this);
                    if (currentActiveUI != null)
                    {
                        currentActiveUI.RemoveState(EUIState.Activating);
                        currentActiveUI.OnActiveChange(false);
                    }
                }
            }
            else
            {
                RemoveState(EUIState.Activating);
                PopupHistory();
            }
            OnActiveChange(v);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnActiveChange(bool v)
        {
            if (LoadedUI == null) return;
            if (v)
            {
                if (UIAnim != null)
                    UIAnim.PlayForward(true);
                else
                    LoadedUI.gameObject.SetActive(true);
            }
            else
            {
                if (UIAnim != null)
                    UIAnim.PlayBackward(true);
                else
                    LoadedUI.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Destroy()
        {
            Log.Debug?.Write(UIType.Name, "destroy ui");
            AddState(EUIState.Destroyed);
            PopupHistory();
            if (LoadedUI != null)
            {
                if (UIAnim != null)
                {
                    UIAnim.PlayBackward(false);
                }
                else if (Meta.DestroyImmediate == 2)
                {
                    Object.DestroyImmediate(LoadedUI.gameObject);
                }
                else
                {
                    if (Meta.DestroyImmediate == 1)
                        LoadedUI.gameObject.SetActive(false);
                    Object.Destroy(LoadedUI.gameObject);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void PopupHistory()
        {
            if (Container.History == null || Container.History.Count == 0)
                return;
            if (Container.History[^1] == this)
            {
                var temp = Container.History.ElementAtOrDefault(Container.History.Count - 2);
                if (temp != null)
                {
                    temp.AddState(EUIState.Activating);
                    temp.OnActiveChange(true);
                }
            }
            Container.History.Remove(this);
        }

        public static implicit operator Transform(UIContext ctx) => ctx.LoadedUI.transform;
        public static implicit operator UIBase(UIContext ctx) => ctx.LoadedUI;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckState(EUIState state) => (States & state) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveState(EUIState state) => States &= ~state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddState(EUIState state) => States |= state;
    }

    /// <summary>
    /// 
    /// </summary>
    public class MultiUIContext : UIContext, IEnumerable<MultiUIContext>
    {
        protected internal MultiUIContext Next;
        protected internal MultiUIContext Prev;


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<MultiUIContext> GetEnumerator()
        {
            var ctx = Next;
            while (ctx != null)
            {
                yield return ctx;
                ctx = ctx.Next;
            }
        }

        public override UIContext Reopen()
        {
            var newCtx = (MultiUIContext)TypeAssistant.New(GetType());
            newCtx.Next = this;
            Prev = newCtx;
            return Container.OpenedUIs[UIType] = newCtx.Initialize(Meta, UIType, Container);
        }


        public override void Destroy()
        {
            base.Destroy();
            if (Next != null)
                Container.OpenedUIs[UIType] = Next;
        }
    }
}
