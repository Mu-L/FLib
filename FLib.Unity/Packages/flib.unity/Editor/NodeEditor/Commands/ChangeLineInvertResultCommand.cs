//==================={By Qcbf|qcbf@qq.com|8/28/2021 5:36:32 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using FLib.Unity.Editor;

namespace FLib.Unity.Editor
{
    public class ChangeLineInvertResultCommand : BaseValueUndoCommand<NodeStageEditor, bool>
    {

        public NodeLineEditor Line;

        protected override bool Value
        {
            get => Line.IsInvertResult;
            set => Line.IsInvertResult = value;
        }


        public void Finish(bool v, NodeLineEditor line)
        {
            Line = line;
            base.Finish(v);
        }

    }
}
