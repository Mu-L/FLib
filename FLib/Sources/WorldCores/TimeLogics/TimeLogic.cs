// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.TimeLogics;

namespace FLib.WorldCores.TimeLogics
{
    [BytesPackGenHoldKey(2)]
    [RenamedType("Worlds.TimeLogic.WorldTimeLogicRuntime")]
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
        private FNum _frameDeltaCache;
        public ScriptPackInstance[] Tracks = Array.Empty<ScriptPackInstance>();

        public bool IsEndFrame { get; private set; }
        public int FrameCount => EndFrame + 1;
        public FNum Duration => EndFrame * FrameDelta;
        public FNum FrameDelta => FNum.One / FrameRate;
        public bool IsInitialized => Tracks != null;
        public ExternalReferenceStorer ExternalReferences;
        public TimeLogicTrack this[int index] => Tracks[index].Instance as TimeLogicTrack;

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
        public virtual TimeLogic Initialize()
        {
            _frameDeltaCache = FrameDelta;
            Tracks ??= Array.Empty<ScriptPackInstance>();
            foreach (var item in Tracks)
                ((TimeLogicTrack)item.Instance).Initialize(this);
            return this;
        }

        /// <summary>
        ///  
        /// </summary>
        public TimeLogic SetFrameRate(byte frameRate)
        {
            _frameDeltaCache = FrameDelta;
            FrameRate = frameRate;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop(bool isResetFrame = true)
        {
            System.Diagnostics.Debug.Assert(Tracks != null, "not initialized");
            if (isResetFrame)
                _currentFrame = 0;
            foreach (var track in Tracks)
            {
                try
                {
                    ((TimeLogicTrack)track.Instance).Stop();
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
            _currentFrame += frameDelta / _frameDeltaCache;
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
            System.Diagnostics.Debug.Assert(IsInitialized, "not initialized");
            IsEndFrame = false;
            foreach (var pack in Tracks)
            {
                var track = (TimeLogicTrack)pack.Instance;
                if (!track.IsDisable && ExecuteVerifyHandler?.Invoke(track) != false)
                    track.OnUpdate();
            }

            foreach (var pack in Tracks)
            {
                var track = (TimeLogicTrack)pack.Instance;
                if (!track.IsDisable && ExecuteVerifyHandler?.Invoke(track) != false)
                    track.OnLateUpdate();
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
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class EntityTimeLogic : TimeLogic
    {
        [NonSerialized] public WorldEntity Entity;
        public WorldCore World => Entity.World;

        public EntityTimeLogic Initialize(WorldEntity entity)
        {
            base.Initialize();
            Entity = entity;
            return this;
        }
    }
}
