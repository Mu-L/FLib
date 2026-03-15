using System;
using Cysharp.Threading.Tasks;
using FLib;
using FLib.Unity;
using UnityEngine;

namespace Modules.Loading
{
    [ModuleUI(DefaultLayer = (int)EUILayer.Popup)]
    public class LoadingUI : UIBase<LoadingUI.Context>
    {
        public UIProgress Progress;

        public class Context : UIContext
        {
            public float Progress;
        }

        private void Start()
        {
            Progress.Value = SelfContext.Progress;
        }

        private void Update()
        {
            if (Progress.Value >= 0.99f)
                Close();
            else if (Progress.Value == SelfContext.Progress)
                return;

            if (SelfContext.Progress >= 1 && Progress.Value < 1)
                Progress.Value = 1;
            else
                Progress.Value = Mathf.Lerp(Progress.Value, SelfContext.Progress, 3 * Time.deltaTime);
        }

        /// <summary>
        /// 
        /// </summary>
        public static Context Show(float progress)
        {
            var ctx = (Context)UIMgr.Open<LoadingUI>();
            ctx.Progress = progress;
            if (ctx.LoadedUI is LoadingUI ui && ui.Progress.Value > progress)
                ui.Progress.Value = progress;
            return ctx;
        }
    }
}
