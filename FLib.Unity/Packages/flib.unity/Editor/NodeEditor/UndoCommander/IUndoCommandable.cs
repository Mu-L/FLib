//==================={By Qcbf|qcbf@qq.com|6/6/2021 5:21:00 PM}===================

namespace FLib.Unity.Editor
{
    public interface IUndoCommandable
    {
        object Owner { get; set; }
        UndoCommander Commander { get; set; }
        void Finish();
        void Initialize();
        void OnBegin();
        void OnEnd();
    }
}
