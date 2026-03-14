using UnityEngine;

namespace FLib.Unity
{
    public class UIBase : MonoBehaviour
    {
        public UIContext SelfContext;
        public UIClickable[] CloseButtons;

        protected internal virtual void InitializeUI()
        {
            if (!(CloseButtons?.Length > 0)) return;
            foreach (var btn in CloseButtons)
            {
                if (btn == null)
                    Log.Error?.Write($"{transform.GetTransformPath()} close btn is null");
                else
                    btn.SetClickHandle(OnClickCloseButton);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnClickCloseButton(UIClickable arg0)
        {
            Close();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Close()
        {
            if (!SelfContext.CheckState(EUIState.Destroyed))
                SelfContext.Container.Close(SelfContext);
        }
    }

    public abstract class UIBase<TContext> : UIBase where TContext : UIContext
    {
        public new TContext SelfContext => (TContext)base.SelfContext;
    }
}
