using System;
using UnityEngine;

namespace FLib.Unity
{
    [ExecuteAlways]
    public abstract class UIAnimElement : MonoBehaviour, ICanvasRaycastFilter
    {
        public UIAnim Anim;
        public abstract void UpdateProcess(float progress);
        public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) => Anim.IsPlayForward;

        protected virtual void Awake()
        {
            Anim ??= GetComponentInParent<UIAnim>();
            Anim?.Elements?.Add(this);
        }

        protected virtual void OnDestroy()
        {
            Anim?.Elements?.Remove(this);
        }

        public virtual void SetActive(bool value)
        {
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!Application.isPlaying && Anim == null)
                Anim = GetComponentInParent<UIAnim>(true);
        }
#endif
    }
}
