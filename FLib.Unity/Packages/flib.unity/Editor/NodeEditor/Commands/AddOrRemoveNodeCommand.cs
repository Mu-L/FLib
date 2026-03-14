//==================={By Qcbf|qcbf@qq.com|6/13/2021 3:26:38 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FLib.Unity.Editor
{
    public class AddOrRemoveTargetsCommand : BaseUndoCommand<NodeStageEditor>
    {
        public IEnumerable<INodeSelectableEditor> Targets;
        public bool IsAdd;


        public void Finish(bool isAdd, IEnumerable<INodeSelectableEditor> targets)
        {
            Targets = targets;
            IsAdd = isAdd;
            base.Finish();
        }

        public override void OnBegin()
        {
            if (IsAdd)
            {
                Owner.AddTargets(Targets);
            }
            else
            {
                Owner.RemoveTargets(Targets);
            }
        }

        public override void OnEnd()
        {
            if (IsAdd)
            {
                Owner.RemoveTargets(Targets);
            }
            else
            {
                Owner.AddTargets(Targets);
            }
        }


    }
}
