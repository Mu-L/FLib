//==================={By Qcbf|qcbf@qq.com|6/13/2021 5:01:23 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using FLib.Unity.Editor;
using UnityEngine;

namespace FLib.Unity.Editor
{
    public class CreateLineCommand : BaseUndoCommand<NodeStageEditor>
    {
        public NodeLineEditor Line;
        public INodeSelectableEditor[] RightNode;


        public void Finish(NodeLineEditor line, Vector2 mousePosition)
        {
            Line = line;
            if (Line.RightUid == 0)
            {
                var node = line.Stage.CreateNode(mousePosition);
                Line.RightUid = node.Uid;
                RightNode = new INodeSelectableEditor[] { node };
            }
            base.Finish();
        }

        public override void OnBegin()
        {
            if (RightNode != null)
            {
                Line.Stage.AddTargets(RightNode);
            }
            Line.AddToLeft();
        }

        public override void OnEnd()
        {
            if (RightNode != null)
            {
                Line.Stage.RemoveTargets(RightNode);
            }
            Line.RemoveFromLeft();
        }

    }
}
