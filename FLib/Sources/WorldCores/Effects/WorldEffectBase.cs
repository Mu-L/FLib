// ==================== qcbf@qq.com | 2026-03-09 ====================

using System;
using FLib.WorldCores.Entities;
using FLib.WorldCores.SoaComponents;

namespace FLib.WorldCores.Effects
{
    [BytesPackGenHoldKey(2)]
    public abstract unsafe class WorldEffectBase : IBytesPackable
    {
        [NonSerialized] internal WorldEffectSystem* SystemPtr;
        public uint Id;
        public WorldEntityId AddedBy;
        public ushort MaxStackCount;
        public ushort StackCount;
        public FNum Duration;
        public EWorldEffectAddOption AddOption;
        public WorldSoaComponentManaged ComponentManaged;
        [NonSerialized] internal int TimeComponentId = -1;
        
        public abstract uint FlagsMask { get; }
        public ref WorldEffectSystem System => ref *SystemPtr;
        public ref WorldEntity Entity => ref SystemPtr->Entity;
        public WorldCore World => SystemPtr->Entity.World;
        public bool IsEmpty => SystemPtr == null;
        public ref WorldEffectTime Time => ref World.Soa.GetGroup<WorldEffectTime>()[TimeComponentId];
        
        /// <summary>
        /// 
        /// </summary>
        public bool IsRemoving
        {
            get => Duration == -1;
            set
            {
                World.Assert(value);
                Duration = -1;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override string ToString() => Json5.Serialize(this);
        
        /// <summary>
        /// 
        /// </summary>
        public virtual bool Check(in WorldEntityId createBy, uint id, int addCount)
        {
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public virtual void OnAwake()
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
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
        public virtual void Dispose()
        {
            TimeComponentId = -1;
            ComponentManaged.Dispose();
            MaxStackCount = StackCount = 0;
            Duration = default;
            AddOption = default;
            SystemPtr = null;
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