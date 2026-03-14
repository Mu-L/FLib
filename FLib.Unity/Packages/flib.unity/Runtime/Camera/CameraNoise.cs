// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using Cysharp.Threading.Tasks;
using FLib.Unity;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    [ExecuteAlways]
    public class CameraNoise : CinemachineExtension
    {
        public Vector3 PivotOffset;

        private TransformParam _position;
        private TransformParam _rotation;
        private float _duration;
        private float _timeElapsed;
        private Damping _damping;


        [Serializable]
        public struct Param
        {
            [Comment("频率")] public float Frequency;
            [Comment("幅度")] public float Amplitude;
            public override string ToString() => $"{Frequency},{Amplitude}";

            public Param(float frequency, float amplitude)
            {
                Frequency = frequency;
                Amplitude = amplitude;
            }

            public float Get(float time) => Mathf.Cos(Frequency * time * 2 * Mathf.PI) * Amplitude * 0.5f;
            public static implicit operator float2(in Param p) => new(p.Frequency, p.Amplitude);
            public static implicit operator Param(in float2 p) => new(p.x, p.y);
        }

        [Serializable]
        public struct TransformParam
        {
            public Param X;
            public Param Y;
            public Param Z;
            public Vector3 Get(float time) => new(X.Get(time), Y.Get(time), Z.Get(time));
            public static implicit operator TransformParam(in NoiseSettings.TransformNoiseParams v) => new() { X = new Param(v.X.Frequency, v.X.Amplitude), Y = new Param(v.Y.Frequency, v.Y.Amplitude), Z = new Param(v.Z.Frequency, v.Z.Amplitude) };
            public override string ToString() => $"{X}|{Y}|{Z}";
        }

        [Serializable]
        public struct Damping
        {
            public FTweenAnimation.EEaseType Ease;
            public TransformParam Position;
            public TransformParam Rotation;
            public bool IsEmpty => Ease == FTweenAnimation.EEaseType.None;
            public void Empty() => Ease = FTweenAnimation.EEaseType.None;
            public override string ToString() => $"({Position})({Rotation})";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _duration = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState curState, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Noise || _timeElapsed >= _duration)
                return;

            if (!_damping.IsEmpty)
            {
                var t = FTweenAnimation.Tween(_damping.Ease, (FNum)(_timeElapsed / _duration));
                var vec = math.lerp(new float3(_damping.Position.X.Amplitude, _damping.Position.Y.Amplitude, _damping.Position.Z.Amplitude), float3.zero, t);
                _position.X.Amplitude = vec.x;
                _position.Y.Amplitude = vec.y;
                _position.Z.Amplitude = vec.z;
                vec = math.lerp(new float3(_damping.Rotation.X.Amplitude, _damping.Rotation.Y.Amplitude, _damping.Rotation.Z.Amplitude), float3.zero, t);
                _rotation.X.Amplitude = vec.x;
                _rotation.Y.Amplitude = vec.y;
                _rotation.Z.Amplitude = vec.z;
            }
            curState.PositionCorrection += curState.GetCorrectedOrientation() * _position.Get(_timeElapsed);
            var rotNoise = Quaternion.Euler(_rotation.Get(_timeElapsed));
            _timeElapsed += Time.deltaTime;
            if (PivotOffset != Vector3.zero)
            {
                var m = Matrix4x4.Translate(-PivotOffset);
                m = Matrix4x4.Rotate(rotNoise) * m;
                m = Matrix4x4.Translate(PivotOffset) * m;
                curState.PositionCorrection += curState.GetCorrectedOrientation() * m.MultiplyPoint(Vector3.zero);
            }
            curState.OrientationCorrection *= rotNoise;
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodButton]
        public void Shake(in TransformParam pos, in TransformParam rot, float duration, FTweenAnimation.EEaseType damping = FTweenAnimation.EEaseType.None)
        {
            _position = pos;
            _rotation = rot;
            _timeElapsed = 0;
            _duration = duration;
            _damping = new Damping() { Ease = damping };
            if (damping != FTweenAnimation.EEaseType.None)
            {
                _damping.Position = pos;
                _damping.Rotation = rot;
            }
        }
    }
}