// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores.TimeLogics
{
    [BytesPackGenHoldKey(2), Comment("基础轨道")]
    public class TimeLogicTrack : IBytesPackable
    {
        [Comment("名称")] public string Name;
        [Comment("是否禁用")] public bool IsDisable;
        public ScriptPackInstance<TimeLogicClip>[] Clips = Array.Empty<ScriptPackInstance<TimeLogicClip>>();
        
        [NonSerialized] public TimeLogic Runtime;
        
        public TimeLogicClip CurrentClip { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public virtual void Stop()
        {
            ClearCurrentClip();
        }
        
        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearCurrentClip()
        {
            if (CurrentClip == null)
                return;
            var temp = CurrentClip;
            CurrentClip = null;
            temp.End();
        }
        
        /// <summary>
        /// 
        /// </summary>
        public virtual void Update()
        {
            var frame = Runtime.CurrentFrame;
            if (CurrentClip != null)
            {
                try
                {
                    if (CurrentClip.CheckFrame(frame))
                    {
                        CurrentClip.Update();
                        return;
                    }
                    
                    CurrentClip.End();
                    CurrentClip = null;
                }
                catch (Exception e)
                {
                    Log.Error?.Write($"{Runtime.Name} {CommentAttribute.TryGetLabel(CurrentClip?.GetType())} {Runtime.UserData} {e}");
                }
            }
            
            foreach (var pack in Clips)
            {
                var clip = pack.Instance;
                if (!clip.IsDisable && Runtime.ExecuteVerifyHandler?.Invoke(clip) != false && clip.CheckFrame(frame))
                {
                    try
                    {
                        CurrentClip = clip;
                        CurrentClip.Begin();
                        CurrentClip.Update();
                    }
                    catch (Exception e)
                    {
                        Log.Error?.Write($"{Runtime.Name} {CommentAttribute.TryGetLabel(CurrentClip?.GetType())} {Runtime.UserData} {e}");
                    }
                    
                    break;
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LateUpdate()
            => CurrentClip?.LateUpdate();
        
#if UNITY_PROJ
        public T GetExternalReference<T>(in ExternalReferenceField<T> field) where T : class => field.Index < 0 ? null : Runtime.ExternalReferences[field.Index] as T;
        public bool TryGetExternalReference<T>(in ExternalReferenceField<T> field, out T val) where T : class => (val = GetExternalReference(field)) != null;
        public T GetSelfOrExternalReference<T>(in ExternalReferenceField<T> target) where T : class
        {
            if (target.Index >= 0)
                return GetExternalReference(target);
            return Runtime.UserData as T;
        }
#endif
        public virtual void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer)
        {
            key.Push(ref writer, 1);
            writer.Push(IsDisable);
            writer.Push(Name);
            BytesPack.Pack(Clips, ref writer);
        }
        
        public virtual void Z_BytesPackRead(int key, ref BytesReader reader)
        {
            if (key == 1)
            {
                IsDisable = reader.Read<bool>();
                Name = reader.ReadString();
                BytesPack.Unpack(ref Clips, ref reader);
                foreach (var clip in Clips)
                    clip.Instance.Track = this;
            }
        }
    }
}