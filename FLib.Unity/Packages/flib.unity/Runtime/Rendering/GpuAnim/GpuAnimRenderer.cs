// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FLib;
using FLib.Unity;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    /// <summary>
    /// 渲染器
    /// </summary>
    public class GpuAnimRenderer : MonoBehaviour
    {
        private static readonly int ShaderInstanceIdOffset = Shader.PropertyToID("_InstanceIdOffset");
        public Mesh RenderMesh;
        public Material RenderMaterial;
        public Texture2D AnimTex;
        public ShadowCastingMode ShadowMode = ShadowCastingMode.Off;
        public bool IsReceiveShadow;
        public Material ExtraRenderPlaneMaterial;
        public float ExtraRenderPlaneSize = 1f;

        [Header("by baker")]
        public float4x4 RootTransform;

        public GpuAnimClip[] AnimClips = Array.Empty<GpuAnimClip>();
        public string[] HoldBoneNames = Array.Empty<string>();

        public GpuAnimState[] States = new GpuAnimState[2048];

        private int[] _currentFrameVertexIndexes;
        private ComputeBuffer _playAnimClipFrameIndexBuffer;
        private RenderParams _renderParams;
        private Matrix4x4[] _renderMatrices = new Matrix4x4[1023];
        private ReadOnlyDictionary<string, int> _boneIndexCache;
        private Stack<int> _frees;
        private int _renderMeshVertexCount;
        private Mesh _extraRenderPlaneMesh;
        private RenderParams _extraRenderParams;

        public int Count
        {
            get;
            private set;
        }

        public ref GpuAnimState this[int index] => ref States[index];

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            _playAnimClipFrameIndexBuffer?.Dispose();
            if (_extraRenderPlaneMesh != null)
                Destroy(_extraRenderPlaneMesh);
        }

        /// <summary>
        /// 
        /// </summary>
        public GpuAnimRenderer Initialize()
        {
            _renderMeshVertexCount = RenderMesh.vertexCount;
            _renderParams = new RenderParams(RenderMaterial) { matProps = new MaterialPropertyBlock(), shadowCastingMode = ShadowMode, receiveShadows = IsReceiveShadow };
            _renderParams.matProps.SetTexture("_AnimTex", AnimTex);
            _boneIndexCache = new ReadOnlyDictionary<string, int>(HoldBoneNames.Select(((n, i) => (n, i))).ToDictionary(k => k.n, v => v.i));

            if (!ExtraRenderPlaneMaterial)
                return this;
            if (_extraRenderPlaneMesh != null)
                Destroy(_extraRenderPlaneMesh);
            (_extraRenderPlaneMesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-0.5f * ExtraRenderPlaneSize, -0.5f * ExtraRenderPlaneSize, 0.01f),
                    new Vector3(0.5f * ExtraRenderPlaneSize, -0.5f * ExtraRenderPlaneSize, 0.01f),
                    new Vector3(0.5f * ExtraRenderPlaneSize, 0.5f * ExtraRenderPlaneSize, 0.01f),
                    new Vector3(-0.5f * ExtraRenderPlaneSize, 0.5f * ExtraRenderPlaneSize, 0.01f)
                },
                triangles = new[] { 0, 1, 3, 1, 2, 3 },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }
            }).RecalculateNormals();
            _extraRenderParams = new RenderParams(ExtraRenderPlaneMaterial) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddCapacity(int count = 1)
        {
            var newCount = (States?.Length).GetValueOrDefault() + count;
            _frees ??= new Stack<int>(newCount);
            for (var i = (States?.Length).GetValueOrDefault(); i < newCount; i++)
                _frees.Push(i);
            Array.Resize(ref States, newCount);
            Array.Resize(ref _currentFrameVertexIndexes, newCount);
            _playAnimClipFrameIndexBuffer?.Dispose();
            _playAnimClipFrameIndexBuffer = new ComputeBuffer(newCount, UnsafeUtility.SizeOf<int>());
            _renderParams.matProps.SetBuffer("_AnimClipFrameIndices", _playAnimClipFrameIndexBuffer);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Alloc()
        {
            // ReSharper disable once InlineOutVariableDeclaration  因为unity 2021 windows会报错
            // ReSharper disable once RedundantAssignment  因为unity 2021 windows会报错
            var index = 0;
            if (_frees == null || !_frees.TryPop(out index))
            {
                AddCapacity(Mathf.Clamp(Count >> 1, 64, 4096));
                index = _frees!.Pop();
            }
            ++Count;
            States[index].Speed = 1f;
            States[index].Used = true;
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Release(int index)
        {
            --Count;
            States[index] = default;
            (_frees ??= new Stack<int>()).Push(index);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Update()
        {
            if (Count == 0) return;
            var deltaTime = Time.deltaTime;
            var renderCount = 0;
            var offset = 0;
            do
            {
                var renderInstanceCount = math.min(Count - renderCount, 1023);
                var renderInstanceIndex = 0;
                _renderParams.matProps.SetInteger(ShaderInstanceIdOffset, renderCount);
                do
                {
                    ref var state = ref States[renderCount + offset];
                    if (!state.Used)
                    {
                        ++offset;
                        continue;
                    }
                    state.AnimTime += deltaTime * state.Speed;
                    _renderMatrices[renderInstanceIndex] = math.mul(float4x4.TRS(state.Position, Quaternion.Euler(state.Rotation), Vector3.one), RootTransform);
                    var clip = AnimClips[state.AnimClipIndex];
                    var vertexIndex = clip.VertexIndex + NextFrame(ref state, clip) * _renderMeshVertexCount;
                    _currentFrameVertexIndexes[renderCount] = vertexIndex;
                    ++renderCount;
                    ++renderInstanceIndex;
                } while (renderInstanceIndex < renderInstanceCount);
                Graphics.RenderMeshInstanced(_renderParams, RenderMesh, 0, _renderMatrices, renderInstanceCount);
                Graphics.RenderMeshInstanced(_extraRenderParams, _extraRenderPlaneMesh, 0, _renderMatrices, renderInstanceCount);
            } while (renderCount < Count);
            _playAnimClipFrameIndexBuffer.SetData(_currentFrameVertexIndexes, 0, 0, renderCount);
        }

        /// <summary>
        /// 计算下一帧
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NextFrame(ref GpuAnimState state, in GpuAnimClip clip)
        {
            var frame = state.GetAnimFrame(clip.FrameRate);
            if (frame >= clip.FrameCount)
            {
                if (clip.IsLoop)
                {
                    frame = 0;
                    state.AnimTime = 0;
                }
                else
                {
                    frame = clip.FrameCount - 1;
                }
            }
            return frame;
        }

        /// <summary>
        /// 
        /// </summary>
        public float2x3 GetBoneTR(int index, string boneName)
        {
            ref readonly var state = ref States[index];
            var clip = AnimClips[state.AnimClipIndex];
            return clip.Bones[_boneIndexCache[boneName]].TRFrames[state.GetAnimFrame(clip.FrameRate)];
        }
    }

    /// <summary>
    /// 每个动画剪辑
    /// </summary>
    [Serializable]
    public struct GpuAnimClip
    {
        public int VertexIndex;
        public int FrameCount;
        public int FrameRate;
        public bool IsLoop;
        [HideInInspector] public GpuAnimClipBone[] Bones;
    }

    /// <summary>
    /// 动画剪辑每一帧骨骼的运动
    /// </summary>
    [Serializable]
    public struct GpuAnimClipBone
    {
        public float2x3[] TRFrames;
    }

    /// <summary>
    /// 动画的播放状态
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Auto)]
    public struct GpuAnimState
    {
        public bool Used;
        public byte AnimClipIndex;
        public float Speed;
        public float AnimTime;
        public Vector3 Position;
        public Vector3 Rotation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetAnimFrame(float frameRate) => (int)(AnimTime * frameRate);
    }
}
