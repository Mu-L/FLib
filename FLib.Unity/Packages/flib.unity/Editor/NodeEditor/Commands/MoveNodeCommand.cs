//==================={By Qcbf|qcbf@qq.com|6/13/2021 4:34:50 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Linq;
using FLib.Unity.Editor;
using UnityEngine;

namespace FLib.Unity.Editor
{
    public class MoveNodeCommand : BaseUndoCommand<NodeStageEditor>
    {
        public Data[] Datas;

        public struct Data
        {
            public NodeEditor Node;
            public Vector2 OldPos;
            public Vector2 NewPos;
        }

        public MoveNodeCommand SetData(IEnumerable<NodeEditor> nodes)
        {
            Datas = nodes.Select(node => new Data { Node = node, OldPos = node.Position }).ToArray();
            return this;
        }

        public override void Finish()
        {
            for (var i = 0; i < Datas.Length; i++)
            {
                Datas[i].NewPos = Datas[i].Node.FormatPosition;
            }
            base.Finish();
        }

        public override void OnBegin()
        {
            for (var i = 0; i < Datas.Length; i++)
            {
                Datas[i].Node.Position = Datas[i].NewPos;
            }
        }

        public override void OnEnd()
        {
            for (var i = 0; i < Datas.Length; i++)
            {
                Datas[i].Node.Position = Datas[i].OldPos;
            }
        }

    }
}
