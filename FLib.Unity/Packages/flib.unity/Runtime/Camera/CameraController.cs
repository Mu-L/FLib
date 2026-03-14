// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace FLib.Unity
{
    [DefaultExecutionOrder(-1)]
    public class CameraController<T> : MonoBehaviour where T : CameraController<T>
    {
        public static readonly List<T> Cameras = new();
        public static T Active => Cameras.ElementAtOrDefault(Cameras.Count - 1);
        public static Transform TrackingTarget { get; private set; }

        [HideInInspector] public CameraNoise Noise;
        public CinemachineCamera VCam;
        public EOption Options;

        [Flags]
        public enum EOption
        {
            None,
            IsIgnoreBlend = 1,
            FromLastCameraTransform = 1 << 1,
        }

        protected virtual void OnEnable()
        {
            if ((Options & EOption.FromLastCameraTransform) != 0 && Active != null)
            {
                Active.transform.GetPositionAndRotation(out var pos, out var rot);
                transform.SetPositionAndRotation(pos, rot);
            }
            Cameras.Add((T)this);
            VCam.Target.TrackingTarget = TrackingTarget;
            if ((Options & EOption.IsIgnoreBlend) != 0)
                IgnoreBlend().Forget();
        }

        protected virtual void OnDisable()
        {
            Cameras.Remove((T)this);
        }

        /// <summary>
        /// 
        /// </summary>
        public async UniTaskVoid IgnoreBlend()
        {
            for (var i = 0; i < 15 && this != null && gameObject.activeInHierarchy; i++)
            {
                await UniTask.Yield();
                CinemachineBrain.GetActiveBrain(0).ActiveBlend = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void SetTrackingTarget(Transform target, bool isImmediate = false)
        {
            TrackingTarget = target;
            if (Cameras.Count > 0)
            {
                var vCam = Cameras[^1].VCam;
                vCam.Target.TrackingTarget = target;
                if (isImmediate)
                    vCam.PreviousStateIsValid = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodButton]
        public virtual void Shake(float duration, CameraNoise.TransformParam pos, CameraNoise.TransformParam rot, FTweenAnimation.EEaseType damping = FTweenAnimation.EEaseType.None)
        {
            if (Noise == null)
                Noise = gameObject.AddComponent<CameraNoise>();
            Noise.Shake(pos, rot, duration, damping);
        }

        private void OnValidate()
        {
            if (VCam == null)
                VCam = GetComponentInChildren<CinemachineCamera>();
        }
    }
}
