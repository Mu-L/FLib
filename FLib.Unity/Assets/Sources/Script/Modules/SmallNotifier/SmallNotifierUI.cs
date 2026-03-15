using System.Collections.Generic;
using FLib.Unity;
using TMPro;
using UnityEngine;
using Utilities;

namespace Modules.SmallNotifier
{
    [ModuleUI(DefaultLayer = (int)EUILayer.Popup)]
    public class SmallNotifierUI : UIBase<SmallNotifierUI.Context>
    {
        public Transform Root;
        public SmallNotifierItem Item;

        public class Context : UIContext
        {
            public List<OptionData> Options = new();
        }

        public class OptionData
        {
            public string Text;
            public bool IsClickClose = true;
            public float Duration = 5;
            public bool IsHighlight;
            internal GameObject ItemUI;
            public OptionData(string text) => Text = text;
            public static implicit operator OptionData(string text) => new(text);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Start()
        {
            foreach (var op in SelfContext.Options)
                AddUI(op);
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddUI(OptionData op)
        {
            var item = Instantiate(Item, Root);
            item.gameObject.SetActive(true);
            item.Label.text = op.Text;
            op.ItemUI = item.gameObject;
            if (op.IsHighlight)
            {
                HighlightAnimation.Play(null, item.transform);
            }
            if (op.IsClickClose)
            {
                item.Img.raycastTarget = true;
                item.Click.AddClickHandle(_ => op.Duration = 0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void Update()
        {
            var options = SelfContext.Options;
            for (var i = options.Count - 1; i >= 0; i--)
            {
                if (!((options[i].Duration -= Time.deltaTime) <= 0)) continue;
                if (options.Count == 1)
                {
                    Close();
                    break;
                }
                Destroy(options[i].ItemUI);
                options.RemoveAt(i);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Open(OptionData op)
        {
            var ctx = (Context)UIMgr.Open<SmallNotifierUI>(EUILayer.Popup);
            ctx.Options.Add(op);
            if (ctx.LoadedUI != null)
                ((SmallNotifierUI)ctx.LoadedUI).AddUI(op);
        }
    }
}
