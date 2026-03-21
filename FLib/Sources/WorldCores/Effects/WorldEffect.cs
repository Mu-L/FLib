// ==================== qcbf@qq.com | 2026-03-09 ====================

using FLib.WorldCores.Entities;
using FLib.WorldCores.SoaComponents;

namespace FLib.WorldCores.Effects
{
    [BytesPackGenHoldKey(2)]
    public unsafe class WorldEffect : IBytesPackable
    {
        internal WorldEffectSystem* SystemPtr;
        public WorldEffectData Data;
        internal int TimeComponentId = -1;
        public WorldEntity AddedBy;
        public WorldSoaComponentManaged ComponentManaged;

        public uint Id => Data.Id;
        public ref WorldEffectSystem System => ref *SystemPtr;
        public ref WorldEntityHelper Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsEmpty => SystemPtr == null;
        public ref WorldEffectTime Time => ref World.Soa.GetGroup<WorldEffectTime>()[TimeComponentId];

        /// <summary>
        /// 
        /// </summary>
        public bool IsRemoving
        {
            get => Data.Duration == -1;
            set
            {
                World.Assert(value);
                Data.Duration = -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString() => Data.ToString();

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
        public virtual void OnAwake()
        {
        }

        public virtual void OnDestroy()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnStackCountChange(int addCount)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void RemoveSelf(ushort removeCount = ushort.MaxValue)
        {
            System.Remove(this, removeCount);
        }

        public virtual void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer)
        {
            key.Push(ref writer, 1);
        }

        public virtual void Z_BytesPackRead(int key, ref BytesReader reader)
        {
        }
    }
}