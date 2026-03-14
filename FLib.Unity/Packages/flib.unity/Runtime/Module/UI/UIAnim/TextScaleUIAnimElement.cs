// ==================== qcbf@qq.com | 2025-08-08 ====================

using FLib;
using TMPro;

namespace FLib.Unity
{
    public class TextScaleUIAnimElement : ScaleUIAnimElement
    {
        public TMP_Text TextMeshPro;

        public override void UpdateProcess(float progress)
        {
            base.UpdateProcess(progress);
            TextMeshPro.SetAllDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (TextMeshPro == null) TextMeshPro = GetComponent<TMP_Text>();
        }
#endif
    }
}
