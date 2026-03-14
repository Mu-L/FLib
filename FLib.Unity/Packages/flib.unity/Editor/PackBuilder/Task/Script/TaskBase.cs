// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;

namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public abstract class TaskBase
    {
        [NonSerialized]
        public string Label;

        protected TaskBase() => Label = CommentAttribute.TryGetLabel(GetType());
        public virtual void Execute(Context ctx) { }
        public virtual void LateExecute(Context ctx) { }
        public virtual void FinishExecute(Context ctx) { }
    }
}
