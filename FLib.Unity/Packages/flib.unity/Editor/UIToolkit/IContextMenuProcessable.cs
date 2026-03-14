// ==================== qcbf@qq.com | 2025-07-01 ====================

using UnityEditor;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public interface IContextMenuProcessable
    {
        void ContextMenuProcess(MouseUpEvent evt, GenericMenu menu);

        public static void RegisterRightContextMenu(VisualElement el)
        {
            el.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button != 1) return;
                GenericMenu menu = null;
                var root = evt.currentTarget;
                var cur = evt.target as VisualElement;
                while (cur != null && cur != root)
                {
                    if (cur is IContextMenuProcessable process)
                    {
                        menu?.AddSeparator(string.Empty);
                        process.ContextMenuProcess(evt, menu ??= new GenericMenu());
                    }
                    cur = cur.parent;
                }
                (root as IContextMenuProcessable)?.ContextMenuProcess(evt, menu ??= new GenericMenu());
                menu?.ShowAsContext();
            });
        }
    }
}
