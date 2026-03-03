// ==================== qcbf@qq.com | 2026-03-02 ====================

namespace FLib.WorldCores.Behaviors
{
    public abstract class BehaviorScript
    {
        public abstract uint Mask { get; }
        public virtual int GetPriority(Entity entity) => 0;
    }
}