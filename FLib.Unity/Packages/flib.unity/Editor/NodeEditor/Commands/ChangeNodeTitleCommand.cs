//==================={By Qcbf|qcbf@qq.com|6/27/2021 5:53:56 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using FLib.Unity.Editor;

namespace FLib.Unity.Editor
{
    public class ChangeNodeTitleCommand : BaseValueUndoCommand<NodeStageEditor, string>
    {
        public NodeEditor Target;

        protected override string Value
        {
            get => Target.Title;
            set => Target.Title = value;
        }

        public void Finish(NodeEditor target, string newTitle)
        {
            Target = target;
            base.Finish(newTitle);
        }

    }
}
