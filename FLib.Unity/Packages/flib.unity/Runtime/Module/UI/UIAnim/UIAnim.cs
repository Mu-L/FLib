// ==================== qcbf@qq.com | 2025-08-08 ====================

using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace FLib.Unity
{
    [DefaultExecutionOrder(-1)]
    public class UIAnim : MonoBehaviour, IUIAnimatable
    {
        public static readonly int ShaderPropertyAnimProgress = Shader.PropertyToID("_AnimProgress");

        private static Stack<Material> _animMatPool = new();

        public float Duration = 0.5f;
        public byte ManageMonoBehaviorType;
        private bool _thenDisableOrDestroy;
        private float _frameStep;
        public bool IsPlayForward => _frameStep > 0;
        public Material AnimMat { get; private set; }
        public bool IsPlaying { get; private set; }

        public HashSet<UIAnimElement> Elements;

        private void Awake()
        {
            Elements = GlobalObjectPool<HashSet<UIAnimElement>>.Create();
            _frameStep = 1f / Duration;
        }

        private void OnDestroy()
        {
            GlobalObjectPool<HashSet<UIAnimElement>>.Release(Elements);
            Elements.Clear();
            Elements = null;
        }

        public void PlayForward(bool withActiveGameObject)
        {
            if (withActiveGameObject && !gameObject.activeSelf)
                gameObject.SetActive(true);
            if (ManageMonoBehaviorType == 2)
            {
                using var pool = ListPool<MonoBehaviour>.Get(out var list);
                GetComponentsInChildren(list);
                foreach (var item in list)
                    item.enabled = true;
            }
            _frameStep = Mathf.Abs(_frameStep);
            if (!IsPlaying)
                UpdateAnim(0f).Forget();
        }

        public void PlayBackward(bool trueDisableOrFalseDestroy)
        {
            if (ManageMonoBehaviorType >= 1)
            {
                using var pool = ListPool<MonoBehaviour>.Get(out var list);
                GetComponentsInChildren(list);
                foreach (var item in list)
                {
                    if (item is not (UIAnim or UIAnimElement))
                        item.enabled = false;
                }
            }
            _frameStep = -Mathf.Abs(_frameStep);
            _thenDisableOrDestroy = trueDisableOrFalseDestroy;
            if (!IsPlaying)
                UpdateAnim(1f).Forget();
        }


        /// <summary>
        /// 
        /// </summary>
        private async UniTask UpdateAnim(float progress)
        {
            IsPlaying = true;
            try
            {
                AnimMat = RentAnimMat();
                foreach (var el in Elements)
                    el.SetActive(true);
                do
                {
                    SetAnimMaterialProgress(AnimMat, progress);
                    foreach (var el in Elements)
                        el.UpdateProcess(progress);
                    await UniTask.NextFrame();
                    progress += Time.deltaTime * _frameStep;
                    if (this == null)
                        return;
                } while (progress < 1 && progress > 0);
                progress = Mathf.Clamp01(progress);
                foreach (var el in Elements)
                    el.UpdateProcess(progress);
                await UniTask.NextFrame();

                foreach (var el in Elements)
                    el.SetActive(false);
            }
            finally
            {
                IsPlaying = false;
                _animMatPool.Push(AnimMat);
                AnimMat = null;
                if (!IsPlayForward && this != null)
                {
                    if (_thenDisableOrDestroy)
                        gameObject.SetActive(false);
                    else
                        Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void SetAnimMaterialProgress(Material mat, float progress)
        {
            mat.SetFloat(ShaderPropertyAnimProgress, progress);
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodButton]
        public void ManageElements()
        {
            var elements = new List<UIAnimElement>();
            ManageElements(transform, elements);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodButton]
        public void ClearElements()
        {
            foreach (var comp in GetComponentsInChildren<UIAnimElement>(true))
                DestroyImmediate(comp);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public void ManageElements(Transform root, List<UIAnimElement> allElements = null)
        {
            if (root.name.Contains("#AnimNo", StringComparison.Ordinal))
                return;
            foreach (var ui in root.Children<RectTransform>(TransformChildEnumerator.EType.Any))
            {
                var originalAnimEl = ui.GetComponent<UIAnimElement>();
                if (originalAnimEl is not (ScaleUIAnimElement or AlphaUIAnimElement))
                {
                    Type uiAnimElType = null;
                    if (ui.name.Contains("#AnimScale", StringComparison.Ordinal))
                        uiAnimElType = typeof(ScaleUIAnimElement);
                    else if (ui.TryGetComponent<TextMeshProUGUI>(out _))
                        uiAnimElType = typeof(TextScaleUIAnimElement);
                    else if (ui.TryGetComponent<Graphic>(out var graphic))
                    {
                        if (ui.name.Contains("#AnimAlpha", StringComparison.Ordinal))
                            uiAnimElType = typeof(AlphaUIAnimElement);
                        else if (graphic.color.a > 0)
                            uiAnimElType = typeof(MaterialUIAnimElement);
                    }

                    if (originalAnimEl?.GetType() != uiAnimElType)
                    {
                        if (originalAnimEl != null)
                            DestroyImmediate(originalAnimEl);
                        if (uiAnimElType != null)
                        {
                            originalAnimEl = (UIAnimElement)ui.gameObject.AddComponent(uiAnimElType);
#if UNITY_EDITOR
                            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(originalAnimEl, false);
                            if (originalAnimEl is MaterialUIAnimElement)
                                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(originalAnimEl))
                                {
                                }
#endif
                        }
                    }
                }
                if (originalAnimEl != null)
                    allElements?.Add(originalAnimEl);

                if (!ui.TryGetComponent<UIAnim>(out _) && !ui.name.Contains("#AnimNoAll", StringComparison.Ordinal))
                    ManageElements(ui, allElements);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static Material RentAnimMat()
        {
            if (!_animMatPool.TryPop(out var mat))
            {
                mat = new Material(Resources.Load<Shader>("SimpleUIAnim"));
                mat.SetTexture("_AnimNoiseTex", Resources.Load<Texture2D>("SimpleUIAnimNoise"));
            }
            return mat;
        }
    }
}
