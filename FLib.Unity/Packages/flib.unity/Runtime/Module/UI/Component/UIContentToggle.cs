// ==================== qcbf@qq.com | 2025-09-05 ====================

using System;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public class UIContentToggle : MonoBehaviour
    {
        public Data[] Datas;
        public int ActiveIndex;

        [Serializable]
        public struct Data
        {
            public UIButton Button;
            public GameObject Page;
        }

        private void Awake()
        {
            for (var i = 0; i < Datas.Length; i++)
            {
                Datas[i].Button.UserData = i;
                Datas[i].Button.AddClickHandle(OnClickToggle);
            }
        }

        private void Start()
        {
            SetActiveIndex(ActiveIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnClickToggle(UIClickable arg0)
        {
            if (((UIButton)arg0).IsDisabled) return;
            var index = (int)arg0.UserData;
            SetActiveIndex(index);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetActiveIndex(int index)
        {
            UIAnim anim;
            if (ActiveIndex != index)
            {
                Datas[ActiveIndex].Button.SetState(UIButton.EState.Disabled, false);
                if (Datas[ActiveIndex].Page.TryGetComponent(out anim))
                    anim.PlayBackward(true);
                else
                    Datas[ActiveIndex].Page.SetActive(false);
            }
            ActiveIndex = index;
            Datas[ActiveIndex].Button.SetState(UIButton.EState.Disabled, true);
            if (!Datas[ActiveIndex].Page.activeSelf)
            {
                if (Datas[ActiveIndex].Page.TryGetComponent(out anim))
                    anim.PlayForward(true);
                else
                    Datas[ActiveIndex].Page.SetActive(true);
            }
        }
    }
}
