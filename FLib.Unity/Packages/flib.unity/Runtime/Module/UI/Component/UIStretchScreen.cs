using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity
{
    public class UIStretchScreen : MonoBehaviour
    {
        public EOption Option = EOption.AspectRatio;

        public enum EOption
        {
            AspectRatio,
            Fill,
            RotationFill,
        }

        private void OnEnable()
        {
            Stretch();
            UIRoot.OnChangeUIScaler += Stretch;
        }

        private void OnDisable()
        {
            UIRoot.OnChangeUIScaler -= Stretch;
        }

        [MethodButton]
        public void Stretch()
        {
            var rectTf = (RectTransform)transform;
            var screenSize = ((RectTransform)rectTf.root).sizeDelta;
            if (Option == EOption.AspectRatio)
            {
                var size = rectTf.sizeDelta;
                var aspect = size.x / size.y;
                if (aspect <= screenSize.x / screenSize.y)
                    rectTf.sizeDelta = new Vector2(screenSize.x, screenSize.x / aspect);
                else
                    rectTf.sizeDelta = new Vector2(screenSize.y * aspect, screenSize.y);
            }
            else if (Option == EOption.Fill)
            {
                rectTf.sizeDelta = new Vector2(screenSize.x, screenSize.y);
            }
            else if (Option == EOption.RotationFill)
            {
                var size = rectTf.sizeDelta;
                if (size.x > size.y == screenSize.x > screenSize.y)
                {
                    rectTf.localEulerAngles = new Vector3(0, 0, 0);
                    rectTf.sizeDelta = new Vector2(screenSize.x, screenSize.y);
                }
                else
                {
                    rectTf.localEulerAngles = new Vector3(0, 0, 90);
                    rectTf.sizeDelta = new Vector2(screenSize.y, screenSize.x);
                }
            }
            rectTf.anchoredPosition = Vector2.zero;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
