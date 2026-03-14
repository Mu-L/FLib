using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity
{
    public class UIProgress : MonoBehaviour
    {
        public Image ProgressGraphic;
        public string TextFormat = "P0";
        public TextMeshProUGUI Label;

        [SerializeField, Range(0, 1)]
        private float _value = 0.8f;

        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                if (Label != null)
                    Label.text = _value.ToString(TextFormat);
                if (ProgressGraphic != null)
                    ProgressGraphic.fillAmount = _value;
            }
        }

        private void OnValidate()
        {
            Value = _value;
        }
    }
}
