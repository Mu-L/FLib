using Cysharp.Threading.Tasks;

namespace FLib.Unity
{
    public abstract class ServiceBase : FEvent
    {
        public abstract void Begin();
        public abstract void End();
    }

    public abstract class ServiceBase<T> : ServiceBase where T : ServiceBase
    {
        public static T Inst { get; internal set; }
        public override void Begin() => Inst = this as T;
        public override void End() => Inst = null;
    }

    public abstract class StageServiceBase<T> : ServiceBase<T>, IModuleStageable where T : StageServiceBase<T>
    {
        public virtual int StageOrderGroup => 0;
        public abstract uint StageIdMask { get; }
        public abstract UniTask OnEnterStage();
        public abstract UniTask OnExitStage();
    }
}
