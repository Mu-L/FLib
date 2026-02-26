// ==================== qcbf@qq.com | 2026-02-26 ====================

namespace FLib.WorldCores
{
    public class UpdateStartSystem : UpdateSystem
    {
        public override void Update(WorldCore world)
        {
            base.Update(world);
            Count = 0;
        }
    }
}