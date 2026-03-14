using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.Serialization;


namespace FLib.Unity
{
    public class UIRadioButton : UIClickable
    {
        [Header("单选组"), Tooltip("相同的组只能选中一个")] public byte Group;
        public static readonly SlimDictionary<byte, UIRadioButton> GroupActives = new SlimDictionary<byte, UIRadioButton>(16);

        [FormerlySerializedAs("IsDefaultSelection")] [Header("是否默认选中")]
        public bool IsDefaultSelection;

        [FormerlySerializedAs("SelectGameObj")] [Header("选中激活预制件,未选中隐藏")]
        public GameObject SelectGameObj;


        public Action<bool, UIRadioButton> OnValueChange;

        public bool IsSelected
        {
            get => GroupActives.GetOrAddValueRef(Group) == this;
            set
            {
                ref var selected = ref GroupActives.GetOrAddValueRef(Group);
                if (selected == this)
                {
                    return;
                }

                var oldSelected = selected;
                selected = this;
                if (oldSelected != null)
                {
                    oldSelected.OnDeselect();
                    oldSelected.OnValueChange?.Invoke(false, oldSelected);
                }

                OnSelect();
                OnValueChange?.Invoke(value, this);
                // ClickCallBack?.Invoke();
            }
        }

        public void OnSelect()
        {
            if (SelectGameObj)
            {
                SelectGameObj.SetActive(true);
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            IsSelected = !IsSelected;
            base.OnPointerClick(eventData);
        }

        public void OnDeselect()
        {
            if (SelectGameObj)
            {
                SelectGameObj.SetActive(false);
            }
        }

    }
}