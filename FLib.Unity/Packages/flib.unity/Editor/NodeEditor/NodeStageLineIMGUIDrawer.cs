//==================={By Qcbf|qcbf@qq.com|8/30/2021 4:20:19 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class NodeStageLineIMGUIDrawer : ImmediateModeElement
    {
        public NodeStageEditor Stage;

        protected override void ImmediateRepaint()
        {
            Stage.TempLine?.DrawLine();
            foreach (var node in Stage.Nodes.Values)
            {
                foreach (var line in node.Lines)
                {
                    if (Stage.Nodes.ContainsKey(line.RightUid))
                    {
                        line.DrawLine();
                    }
                }
            }
        }


    }
}
