//==================={By Qcbf|qcbf@qq.com|8/28/2021 3:25:44 PM}===================

using FLib.Unity.Editor;

namespace FLib.Unity.Editor
{
    public class ChangeLineConnectComand : BaseUndoCommand<NodeStageEditor>
    {

        public NodeLineEditor Line;
        public (uint, uint) NewNodeUid;
        public (uint, uint) OldNodeUid;



        public void Finish(NodeLineEditor line, uint lUid, uint rUid)
        {
            Line = line;
            OldNodeUid = (line.Left.Uid, line.Right.Uid);
            NewNodeUid = (lUid, rUid);
            base.Finish();
        }


        public override void OnBegin()
        {
            Line.Left.RemoveLine(Line);
            Line.Left = Owner.Nodes[NewNodeUid.Item1];
            Line.RightUid = NewNodeUid.Item2;
            Owner.Nodes[NewNodeUid.Item1].AddLine(Line);
        }

        public override void OnEnd()
        {
            Line.Left = Owner.Nodes[OldNodeUid.Item1];
            Line.RightUid = OldNodeUid.Item2;
        }
    }
}
