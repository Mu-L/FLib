// ==================== qcbf@qq.com | 2026-03-09 ====================

using System;
using System.Runtime.InteropServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.SoaComponents;

namespace FLib.WorldCores.Effects
{
    [BytesPackGenHoldKey(2)]
    public abstract unsafe class WorldEffectBase : IBytesPackable
    {
        public uint Id;
        [Comment("最大叠加层数")] public ushort MaxStackCount = 1;
        [Comment("当前叠加层数")] public ushort StackCount;
        [Comment("重复添加方式")] public EWorldEffectAddOption AddOption;
        [Comment("持续时间")] public FNum Duration;

        [NonSerialized] public WorldEntityId SourceEntityId;
        [NonSerialized] public WorldSoaComponentManaged ComponentManaged;
        internal WorldEffectSystem* SystemPtr;
        internal int TimeComponentId = -1;

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
            writer.PushVInt(Id);
            writer.PushVInt(MaxStackCount);
            writer.PushVInt(StackCount);
            writer.Push(AddOption);
            writer.Push(Duration);
        }

        public virtual void Z_BytesPackRead(int key, ref BytesReader reader)
        {
            if (key == 1)
            {
                Id = (uint)reader.ReadVInt();
                MaxStackCount = (ushort)reader.ReadVInt();
                StackCount = (ushort)reader.ReadVInt();
                AddOption = reader.Read<EWorldEffectAddOption>();
                Duration = reader.Read<FNum>();
            }
        }
    }
}