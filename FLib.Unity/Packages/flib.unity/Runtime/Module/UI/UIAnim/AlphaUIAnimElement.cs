// ==================== qcbf@qq.com | 2025-08-08 ====================

using FLib;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity
{
    public class AlphaUIAnimElement : UIAnimElement
    {
        public FTweenAnimation.EEaseType Ease = FTweenAnimation.EEaseType.Linear;
        public Graphic TargetGraphic;
        public float OriginalValue;


        public override void UpdateProcess(float progress)
        {
            var p = FTweenAnimation.Tween(Ease, (FNum)progress);
            var col = TargetGraphic.color;
            col.a = p * OriginalValue;
            TargetGraphic.color = col;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (TargetGraphic == null) TargetGraphic = GetComponent<Graphic>();
            if (TargetGraphic != null) OriginalValue = TargetGraphic.color.a;
        }
#endif
    }
}
