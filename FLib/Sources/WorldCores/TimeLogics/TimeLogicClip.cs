// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;

namespace FLib.WorldCores.TimeLogics
{
    [BytesPackGenHoldKey(2), Comment("基础片段")]
    public class TimeLogicClip : IBytesPackable
    {
        [Comment("名称")] public string Name;
        [Comment("是否禁用")] public bool IsDisable;
        [Comment("开始帧")] public int BeginFrame;
        [Comment("结束帧")] public int EndFrame;

        public TimeLogicTrack Track { get; private set; }
        public TimeLogic Root => Track.Root;
        public int CurrentFrame => Root.CurrentFrame;
        public int CurrentClipFrame => Root.CurrentFrame - BeginFrame;
        public FNum CurrentClipTime => (FNum)CurrentClipFrame / Root.FrameRate;
        public virtual int FrameCount => EndFrame - BeginFrame + 1;
        public bool IsPlaying => !IsDisable && Root.CurrentFrame >= BeginFrame && Root.CurrentFrame <= EndFrame;

        /// <summary>
        /// 
        /// </summary>
        public virtual void Initialize(TimeLogicTrack track)
        {
            Track = track;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnPlay()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnStop()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnUpdate()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnLateUpdate()
        {
        }

        public T GetExternalReference<T>(in ExternalReferenceField<T> field) where T : class =>
            field.Index < 0 || field.Index >= Root.ExternalReferences.GetArraySize() ? null : Root.ExternalReferences[field.Index] as T;

        public bool TryGetExternalReference<T>(in ExternalReferenceField<T> field, out T val) where T : class => (val = GetExternalReference(field)) != null;

        public T GetSelfOrExternalReference<T>(in ExternalReferenceField<T> target) where T : class
        {
            if (target.Index >= 0)
                return GetExternalReference(target);
            return Root.UserData as T;
        }

        // /// <summary>
        // /// 
        // /// </summary>
        // public virtual bool CheckFrame(int frame) => BeginFrame <= frame && EndFrame >= frame;
        //
        // /// <summary>
        // /// 
        // /// </summary>
        // public virtual bool CheckFrame() => CheckFrame(CurrentFrame);

        public virtual void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer)
        {
            key.Push(ref writer, 1);
            writer.Push(Name);
            writer.Push(IsDisable);
            writer.PushVInt(BeginFrame);
            writer.PushVInt(EndFrame);
        }

        public virtual void Z_BytesPackRead(int key, ref BytesReader reader)
        {
            if (key == 1)
            {
                Name = reader.ReadString();
                IsDisable = reader.Read<bool>();
                BeginFrame = (int)reader.ReadVInt();
                EndFrame = (int)reader.ReadVInt();
            }
        }
    }
}