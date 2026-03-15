// ==================== qcbf@qq.com | 2025-07-29 ====================

using FLib;
using FLib.Unity;
using TMPro;

namespace Modules.Dialog
{
    [ModuleUI]
    public class TextDialogUI : DialogUI<TextDialogUI.Context>
    {
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Text;

        public class Context : DialogContext
        {
            public string Title;
            public string Text;
            public TextAlignmentOptions? Alignment;

            public Context SetAlignment(TextAlignmentOptions val)
            {
                Alignment = val;
                return this;
            }
        }

        protected override void Start()
        {
            base.Start();
            Title.text = SelfContext.Title;
            Text.text = SelfContext.Text;
            if (SelfContext.Alignment != null)
                Text.alignment = SelfContext.Alignment.Value;
        }

        public static Context Open(string title, string text)
        {
            var ctx = (Context)UIMgr.Open<TextDialogUI>(EUILayer.PopupPage);
            ctx.Title = title;
            ctx.Text = text;
            return ctx;
        }
    }
}
