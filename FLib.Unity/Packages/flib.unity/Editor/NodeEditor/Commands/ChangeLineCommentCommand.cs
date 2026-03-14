//==================={By Qcbf|qcbf@qq.com|8/28/2021 2:32:59 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using FLib.Unity.Editor;

namespace FLib.Unity.Editor
{
    public class ChangeLineCommentCommand : BaseValueUndoCommand<NodeStageEditor, string>
    {
        public NodeLineEditor Line;

        protected override string Value
        {
            get => Line.Comment;
            set => Line.Comment = value;
        }


        public void Finish(NodeLineEditor line, string newValue)
        {
            Line = line;
            base.Finish(newValue);
        }



    }
}
