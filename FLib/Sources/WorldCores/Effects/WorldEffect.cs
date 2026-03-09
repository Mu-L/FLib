// ==================== qcbf@qq.com | 2026-03-09 ====================

using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    [BytesPackGenHoldKey(2)]
    public unsafe class WorldEffect
    {
        internal WorldEffectSystem* SystemPtr;

        public uint Id;
        public FNum Duration;
        public int MaxStackCount = 1;
        public BitFlags Flags;
        public EWorldEffectAddOption AddOption = EWorldEffectAddOption.ResetTime;

        public ref WorldEffectSystem System => ref *SystemPtr;
        public ref WorldEntityHelper Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsEmpty => SystemPtr == null;

        /// <summary>
        /// 
        /// </summary>
        public virtual bool Check(in WorldEntity createBy, uint id, int addCount)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Awake()
        {
        }

        public virtual void Destroy()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnStackCountChange(int addCount)
        {
        }
    }
}