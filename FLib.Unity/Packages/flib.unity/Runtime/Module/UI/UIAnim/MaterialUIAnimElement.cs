// ==================== qcbf@qq.com | 2025-08-08 ====================

using FLib;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity
{
    public class MaterialUIAnimElement : UIAnimElement, IMaterialModifier
    {
        public Graphic TargetGraphic;
        private int _defaultUIMaterialHash;
        private Material _material;

        protected override void Awake()
        {
            base.Awake();
            _defaultUIMaterialHash = Canvas.GetDefaultCanvasMaterial().GetHashCode();
        }

        public override void SetActive(bool value)
        {
            base.SetActive(value);
            if (TargetGraphic == null) return;
            TargetGraphic.SetMaterialDirty();
            if (value)
            {
                var mat = TargetGraphic.materialForRendering;
                if (mat != Anim.AnimMat && mat.GetHashCode() != _defaultUIMaterialHash && mat.shader == Anim.AnimMat.shader)
                    _material = mat;
            }
            else
            {
                _material = null;
            }
        }

        public override void UpdateProcess(float progress)
        {
            if (_material != null)
                UIAnim.SetAnimMaterialProgress(_material, progress);
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (baseMaterial.GetHashCode() != _defaultUIMaterialHash)
                return baseMaterial;
            return Anim.IsPlaying ? Anim.AnimMat : baseMaterial;
        }


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (TargetGraphic == null) TargetGraphic = GetComponent<Graphic>();
        }
#endif
    }
}
