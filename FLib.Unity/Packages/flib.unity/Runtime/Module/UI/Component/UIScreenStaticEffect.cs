// =================================================={By Qcbf|qcbf@qq.com|2024-11-07}==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FLib.Unity
{
    public class UIScreenStaticEffect : MonoBehaviour
    {
        public bool IsDelayFrame = true;
        public RawImage Target;
        public ScreenEffectStaticTexture.Option Options;
        public ScreenEffectStaticTexture ScreenEffect;

        private void Awake()
        {
            ScreenEffect = new ScreenEffectStaticTexture() { Options = Options };
        }

        private void OnDestroy()
        {
            ScreenEffect.Dispose();
        }

        private void OnEnable()
        {
            Generate();
        }

        [MethodButton]
        public void Generate()
        {
            // var isDelayFrame = IsDelayFrame && GetComponentInParent<UIAsset>().UIAnimation == null;
            // if (isDelayFrame)
            //     transform.parent.gameObject.layer = 0;
            // ScreenEffect.ReleaseRT();
            // GetComponent<RawImage>().texture = ScreenEffect.Generate(new Vector2(UISetting.Inst.UICamera.pixelWidth, UISetting.Inst.UICamera.pixelHeight));
            // if (isDelayFrame)
            //     UniTask.NextFrame().ContinueWith(() => transform.parent.gameObject.layer = UISetting.Inst.gameObject.layer);
        }
    }
}
