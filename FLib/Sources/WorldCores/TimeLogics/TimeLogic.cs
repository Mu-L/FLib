// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.TimeLogics;

namespace FLib.WorldCores.TimeLogics
{
    [BytesPackGenHoldKey(2)]
    public class TimeLogic : IBytesPackable
    {
        [NonSerialized] public object UserData;

        /// <summary>
        /// obj is TimeLogicTrack or TimeLogicClip
        /// 返回 false 表示阻止执行
        /// </summary>
        [NonSerialized] public Func<object, bool> ExecuteVerifyHandler;

        public bool IsLoop = true;
        public int EndFrame;
        public string Name;
        public byte FrameRate = 30;
        private FNum _currentFrame;
        private FNum _frameDelta;
        public ScriptPackInstance<TimeLogicTrack>[] Tracks = Array.Empty<ScriptPackInstance<TimeLogicTrack>>();

        public bool IsEndFrame { get; private set; }
        public int FrameCount => EndFrame + 1;
        public ExternalReferenceStorer ExternalReferences;

        public int CurrentFrame
        {
            get => (int)_currentFrame;
            set
            {
                if (_currentFrame == value)
                    return;
                _currentFrame = value;
                UpdateCurrentFrame();
            }
        }

        public override string ToString() => $"{Name},{CurrentFrame}";

        /// <summary>
        ///  
        /// </summary>
        public TimeLogic SetFrameRate(byte frameRate)
        {
            _frameDelta = FNum.One / frameRate;
            FrameRate = frameRate;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop(bool isResetFrame = true)
        {
            if (isResetFrame)
                _currentFrame = 0;
            foreach (var track in Tracks)
            {
                try
                {
                    track.Instance.Stop();
                }
                catch (Exception e)
                {
                    Log.Error?.Write($"{Name} {CommentAttribute.TryGetLabel(track.Instance?.GetType())} {e}");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateNextFrame(FNum frameDelta)
        {
            var lastFrame = (int)_currentFrame;
            _currentFrame += frameDelta / _frameDelta;
            if (lastFrame == (int)_currentFrame)
                return;
            if (CurrentFrame > EndFrame)
            {
                if (IsLoop)
                    Stop();
                else
                    CurrentFrame = EndFrame;
                UpdateCurrentFrame();
                IsEndFrame = true;
            }
            else
            {
                UpdateCurrentFrame();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateCurrentFrame()
        {
            IsEndFrame = false;
            foreach (var pack in Tracks)
            {
                var track = pack.Instance;
                if (!track.IsDisable && ExecuteVerifyHandler?.Invoke(track) != false)
                    track.Update();
            }

            foreach (var pack in Tracks)
            {
                var track = pack.Instance;
                if (!track.IsDisable && ExecuteVerifyHandler?.Invoke(track) != false)
                    track.LateUpdate();
            }
        }

        public virtual void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer)
        {
            key.Push(ref writer, 1);
            writer.PushVInt(0);
            writer.Push(FrameRate);
            writer.Push(IsLoop);
            writer.PushVInt(EndFrame);
            BytesPack.Pack(Tracks, ref writer);
        }

        public virtual void Z_BytesPackRead(int key, ref BytesReader reader)
        {
            if (key == 1)
            {
                reader.ReadVInt();
                FrameRate = reader.Read<byte>();
                IsLoop = reader.Read<bool>();
                EndFrame = (int)reader.ReadVInt();
                BytesPack.Unpack(ref Tracks, ref reader);
                _frameDelta = FNum.One / FrameRate;
                foreach (var item in Tracks)
                    item.Instance.Runtime = this;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class EntityTimeLogic : TimeLogic
    {
        [NonSerialized] public WorldEntity Entity;
    }
}