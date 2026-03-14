// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FLib;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Inst;
        public static Action OnChangeUIScaler;
        public Canvas RootCanvas;
        public CanvasScaler Scaler;
        public int LayerOrderInterval = 1000;
        public SecondaryData Secondary;
        public LayerDefine[] LayerDefines;

        public static Rect SafeArea;
        public static ScalerData PrimaryScaler;
        public static UIContainer[] Layers;
        public static Camera UICamera => Inst.RootCanvas.worldCamera;
        public static bool IsPrimary => !Inst.Secondary.IsActivated;

        [Serializable]
        public struct LayerDefine
        {
            public string Name;
            public bool EnableHistory;
            public bool UseSafeArea;

            public LayerDefine(string name, bool enableHistory = false, bool useSafeArea = false)
            {
                Name = name;
                EnableHistory = enableHistory;
                UseSafeArea = useSafeArea;
            }

            public override string ToString() => Name;
            public static implicit operator LayerDefine(string name) => new() { Name = name };
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public struct SecondaryData
        {
            public bool IsActivated;
            public string PrefabSuffix;
            public ScalerData Scaler;
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public struct ScalerData
        {
            public CanvasScaler.ScaleMode ScaleMode;
            public CanvasScaler.ScreenMatchMode MatchMode;
            [Range(0, 1)] public float MatchWidthOrHeight;
            public Vector2 Resolution;
            public bool IsLandscape => Resolution.x > Resolution.y;

            public void Apply(CanvasScaler scaler)
            {
                scaler.uiScaleMode = ScaleMode;
                scaler.screenMatchMode = MatchMode;
                scaler.matchWidthOrHeight = MatchWidthOrHeight;
                scaler.referenceResolution = Resolution;
            }

            public void Copy(CanvasScaler scaler)
            {
                ScaleMode = scaler.uiScaleMode;
                MatchMode = scaler.screenMatchMode;
                MatchWidthOrHeight = scaler.matchWidthOrHeight;
                Resolution = scaler.referenceResolution;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Awake()
        {
            Inst = this;
            SafeArea = Screen.safeArea;
            Layers = CreateLayers(LayerDefines);
            Scaler.enabled = true;
            PrimaryScaler.Copy(Scaler);
            if (Secondary.IsActivated)
                Secondary.Scaler.Apply(Scaler);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual UIContainer[] CreateLayers(in LayerDefine[] layerDefines)
        {
            var unityUILayer = LayerMask.NameToLayer("UI");
            var layers = new UIContainer[layerDefines.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                var layerDef = layerDefines[i];
                var layerGo = new GameObject(layerDef.Name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)) { layer = unityUILayer };
                var rtf = layerGo.GetComponent<RectTransform>();
                var layer = layers[i] = new UIContainer() { Root = rtf, UseSafeArea = layerDef.UseSafeArea };
                rtf.SetParent(transform, false);
                rtf.SetSiblingIndex(i);
                rtf.anchorMin = Vector2.zero;
                rtf.anchorMax = Vector2.one;
                rtf.localPosition = Vector2.zero;
                rtf.sizeDelta = Vector2.zero;
                var canvas = layerGo.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.vertexColorAlwaysGammaSpace = true;
                canvas.sortingOrder = (i + 1) * LayerOrderInterval;
                if (layerDef.EnableHistory)
                    layer.History = new List<UIContext>(8);
                layer.RefreshSafeArea();
            }
            return layers;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (RootCanvas == null) RootCanvas = GetComponent<Canvas>();
            if (Scaler == null) Scaler = GetComponent<CanvasScaler>();
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        public static void SetPrimaryScaler(bool value)
        {
            if (value)
            {
                if (!Inst.Secondary.IsActivated)
                    goto exit;
                Inst.Secondary.IsActivated = false;
                PrimaryScaler.Apply(Inst.Scaler);
            }
            else
            {
                if (Inst.Secondary.IsActivated)
                    goto exit;
                Inst.Secondary.IsActivated = true;
                Inst.Secondary.Scaler.Apply(Inst.Scaler);
            }
            SafeArea = Screen.safeArea;
            foreach (var layer in Layers)
                layer.RefreshSafeArea();
            foreach (var layer in Layers)
            {
                foreach (var ui in layer.OpenedUIs)
                    ui.Value.OnSwitchScaler();
            }
            Log.Info?.Write($"set primary scaler: {value}, {Screen.orientation}, safeArea:{SafeArea}");
            OnChangeUIScaler?.Invoke();
            exit: ;
        }
    }
}
