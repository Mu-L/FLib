// using FLib;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using DG.Tweening;
// using TMPro;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
//
// namespace FLib.Unity
// {
//     [ExecuteAlways]
//     public class UIToggle : MonoBehaviour, IPointerClickHandler
//     {
//         public const float ANIM_TIME = 0.25f;
//
//         public UIToggleRoot Root;
//         public AudioClip SoundAudioClip;
//         public bool IsDisableAnim;
//
//         [Header("选中样式")]
//         public Style SelectStyle;
//
//         [Header("未选中样式")]
//         public Style DeselectStyle;
//
//         public GameObject[] BindContents;
//         public UnityEvent<bool> OnSelectEvent;
//         public object UserData;
//
//         private bool _isSelected;
//
//         [Serializable]
//         public struct Style
//         {
//             public GameObject Element;
//
//             public Graphic Image;
//             public Color ImageColor;
//
//             public TextMeshProUGUI Label;
//             public int FontSize;
//             public Color FontColor;
//
//             public Transform ScaleTarget;
//             public float Scale;
//
//             private readonly Color GetImgColor() => Image.color;
//             private readonly void SetImgColor(Color col) => Image.color = col;
//             private readonly Color GetLabelColor() => Label.color;
//             private readonly void SetLabelColor(Color col) => Label.color = col;
//             private readonly float GetLabelSize() => Label.fontSize;
//             private readonly void SetLabelSize(float v) => Label.fontSize = v;
//             private readonly Vector3 GetScale() => ScaleTarget.transform.localScale;
//             private readonly void SetScale(Vector3 v) => ScaleTarget.transform.localScale = v;
//
//             public readonly void Apply(bool isAnim)
//             {
//                 if (Image != null)
//                 {
//                     if (isAnim) DOTween.To(GetImgColor, SetImgColor, ImageColor, ANIM_TIME).SetEase(Ease.OutBack);
//                     else Image.color = ImageColor;
//                 }
//
//                 if (Label != null)
//                 {
//                     if (isAnim)
//                     {
//                         DOTween.To(GetLabelColor, SetLabelColor, FontColor, ANIM_TIME).SetEase(Ease.OutBack);
//                         DOTween.To(GetLabelSize, SetLabelSize, FontSize, ANIM_TIME).SetEase(Ease.OutBack);
//                     }
//                     else
//                     {
//                         Label.fontSize = FontSize;
//                         Label.color = FontColor;
//                     }
//                 }
//
//                 if (ScaleTarget == null) return;
//                 if (isAnim)
//                 {
//                     DOTween.To(GetScale, SetScale, new Vector3(Scale, Scale, Scale), ANIM_TIME).SetEase(Ease.OutBack);
//                 }
//                 else
//                 {
//                     SetScale(new Vector3(Scale, Scale, Scale));
//                 }
//             }
//
//
//             public readonly void TryActiveElement(bool v)
//             {
//                 if (Element != null) Element.SetActive(v);
//             }
//         }
//
//
//         public bool IsSelected
//         {
//             get => _isSelected;
//             set
//             {
//                 if (value == _isSelected) return;
//                 _isSelected = value;
//                 if (value)
//                 {
//                     if (Root != null)
//                     {
//                         if (Root.Selected != null)
//                         {
//                             Root.Selected.IsSelected = false;
//                         }
//
//                         Root.Selected = this;
//                     }
//
//                     Select();
//                 }
//                 else
//                 {
//                     if (Root != null && Root.Selected == this)
//                     {
//                         Root.Selected = null;
//                     }
//
//                     Deselect();
//                 }
//             }
//         }
//
//         private void Start()
//         {
// #if UNITY_EDITOR
//             if (Application.isPlaying)
// #endif
//             {
//                 if (Root != null)
//                 {
//                     if (Root.Selected == this)
//                     {
//                         _isSelected = true;
//                         Select();
//                     }
//                     else
//                     {
//                         Deselect();
//                     }
//                 }
//             }
//         }
//
//         private void OnDestroy()
//         {
// #if UNITY_EDITOR
//             if (Application.isPlaying)
// #endif
//             {
//                 if (IsSelected && Root != null && Root.Selected == this)
//                 {
//                     Root.Selected = null;
//                 }
//             }
//         }
//
//         public void OnPointerClick(PointerEventData eventData)
//         {
//             if (!IsSelected)
//             {
//                 if (SoundAudioClip != null)
//                     Audio.PlayShort(SoundAudioClip);
//                 IsSelected = true;
//             }
//             else if (Root == null)
//             {
//                 if (SoundAudioClip != null)
//                     Audio.PlayShort(SoundAudioClip);
//                 IsSelected = false;
//             }
//         }
//
//
//         public void Select()
//         {
//             SelectStyle.Apply(!IsDisableAnim);
//             DeselectStyle.TryActiveElement(false);
//             SelectStyle.TryActiveElement(true);
//             OnSelectEvent?.Invoke(true);
//             if (Root != null)
//             {
//                 Root.OnSelectEvent?.Invoke(this);
//             }
//
//             if (BindContents == null) return;
//             foreach (var item in BindContents)
//             {
//                 if (item != null) item.SetActive(true);
//             }
//         }
//
//         public void Deselect()
//         {
//             DeselectStyle.Apply(!IsDisableAnim);
//             SelectStyle.TryActiveElement(false);
//             DeselectStyle.TryActiveElement(true);
//             OnSelectEvent?.Invoke(false);
//             if (Root != null)
//             {
//                 Root.OnDeselectEvent?.Invoke(this);
//             }
//
//             if (BindContents == null) return;
//             foreach (var item in BindContents)
//             {
//                 if (item != null) item.SetActive(false);
//             }
//         }
//
//
// #if UNITY_EDITOR
//         [ContextMenu(nameof(ApplySelectStyle))]
//         public void ApplySelectStyle()
//         {
//             SelectStyle.Apply(false);
//             UnityEditor.EditorUtility.SetDirty(this);
//         }
//
//         [ContextMenu(nameof(ApplyDeselectStyle))]
//         public void ApplyDeselectStyle()
//         {
//             DeselectStyle.Apply(false);
//             UnityEditor.EditorUtility.SetDirty(this);
//         }
// #endif
//     }
// }
