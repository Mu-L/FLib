// ==================== qcbf@qq.com | 2025-08-08 ====================

using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public class ScaleUIAnimElement : UIAnimElement
    {
        public FTweenAnimation.EEaseType Ease = FTweenAnimation.EEaseType.OutBack;
        public Vector3 OriginalValue;


        public override void UpdateProcess(float progress)
        {
            var scale = transform.localScale;
            var p = FTweenAnimation.Tween(Ease, (FNum)progress);
            scale.x = p * OriginalValue.x;
            scale.y = p * OriginalValue.y;
            transform.localScale = scale;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            OriginalValue = transform.localScale;
        }
#endif
    }
}
