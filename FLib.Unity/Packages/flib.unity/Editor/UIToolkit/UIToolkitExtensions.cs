//==================={By Qcbf|qcbf@qq.com|8/17/2021 4:47:14 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public static class UIToolkitExtensions
    {
        public static T BackgroundColor<T>(this T target, StyleColor color) where T : VisualElement
        {
            target.style.backgroundColor = color;
            return target;
        }

        public static T Border<T>(this T target, StyleColor color, float width = 1) where T : VisualElement
        {
            target.style.borderTopWidth = target.style.borderBottomWidth = target.style.borderLeftWidth = target.style.borderRightWidth = width;
            target.style.borderTopColor = target.style.borderBottomColor = target.style.borderLeftColor = target.style.borderRightColor = color;
            return target;
        }

        public static T BorderRadius<T>(this T target, StyleLength tl, StyleLength bl, StyleLength br, StyleLength tr) where T : VisualElement
        {
            target.style.borderTopLeftRadius = tl;
            target.style.borderBottomLeftRadius = bl;
            target.style.borderBottomRightRadius = br;
            target.style.borderTopRightRadius = tr;
            return target;
        }

        public static T Display<T>(this T target, bool v) where T : VisualElement
        {
            target.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
            return target;
        }

        public static T FlexGrow<T>(this T target, StyleFloat v) where T : VisualElement
        {
            target.style.flexGrow = v;
            return target;
        }

        public static T FlexWrap<T>(this T target, StyleEnum<Wrap> v) where T : VisualElement
        {
            target.style.flexWrap = v;
            return target;
        }

        public static T FlexShrink<T>(this T target, StyleFloat v) where T : VisualElement
        {
            target.style.flexShrink = v;
            return target;
        }

        public static T FlexDirection<T>(this T target, StyleEnum<FlexDirection> v) where T : VisualElement
        {
            target.style.flexDirection = v;
            return target;
        }

        public static T MinWidth<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.minWidth = v;
            return target;
        }

        public static T MaxWidth<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.maxWidth = v;
            return target;
        }

        public static T WidthPercent<T>(this T target, float v) where T : VisualElement
        {
            target.style.width = new StyleLength(new Length(v, LengthUnit.Percent));
            return target;
        }

        public static T Width<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.width = v;
            return target;
        }

        public static T MinHeight<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.minHeight = v;
            return target;
        }

        public static T MaxHeight<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.maxHeight = v;
            return target;
        }

        public static T Height<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.height = v;
            return target;
        }

        public static T ShortFieldLabel<T>(this T target, StyleLength minWidth = default) where T : VisualElement
        {
            target.ElementAt(0).style.minWidth = StyleKeyword.Initial;
            if (minWidth != default)
                target.ElementAt(1).style.minWidth = minWidth;
            return target;
        }

        public static T TextAlign<T>(this T target, StyleEnum<TextAnchor> v) where T : VisualElement
        {
            target.style.unityTextAlign = v;
            return target;
        }

        public static T Color<T>(this T target, StyleColor v) where T : VisualElement
        {
            target.style.color = v;
            return target;
        }

        public static T FontSize<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.fontSize = v;
            return target;
        }

        #region margin
        public static T Margin<T>(this T target, StyleLength ublr) where T : VisualElement
        {
            target.style.marginRight = target.style.marginLeft = target.style.marginBottom = target.style.marginTop = ublr;
            return target;
        }

        public static T Margin<T>(this T target, StyleLength top, StyleLength bottom, StyleLength left, StyleLength right) where T : VisualElement
        {
            target.style.marginTop = top;
            target.style.marginBottom = bottom;
            target.style.marginLeft = left;
            target.style.marginRight = right;
            return target;
        }

        public static T MarginTop<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.marginTop = v;
            return target;
        }

        public static T MarginBottom<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.marginBottom = v;
            return target;
        }

        public static T MarginLeft<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.marginLeft = v;
            return target;
        }

        public static T MarginRight<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.marginRight = v;
            return target;
        }
        #endregion

        #region padding
        public static T Padding<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.paddingTop = v;
            target.style.paddingBottom = v;
            target.style.paddingLeft = v;
            target.style.paddingRight = v;
            return target;
        }

        public static T Padding<T>(this T target, StyleLength v, StyleLength bottom, StyleLength left, StyleLength right) where T : VisualElement
        {
            target.style.paddingTop = v;
            target.style.paddingBottom = bottom;
            target.style.paddingLeft = left;
            target.style.paddingRight = right;
            return target;
        }

        public static T PaddingTop<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.paddingLeft = v;
            return target;
        }

        public static T PaddingBottom<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.paddingBottom = v;
            return target;
        }

        public static T PaddingLeft<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.paddingLeft = v;
            return target;
        }

        public static T PaddingRight<T>(this T target, StyleLength v) where T : VisualElement
        {
            target.style.paddingRight = v;
            return target;
        }
        #endregion

        public static T MiniButton<T>(this T v) where T : VisualElement =>
            v.Padding(0, 0, 2, 2).Margin(0, 0, 0, 0).BackgroundColor(EditorGUIUtility.isProSkin ? new Color(0.204f, 0.204f, 0.204f) : new Color(0.769f, 0.769f, 0.769f));

        /// <summary>
        /// 
        /// </summary>
        public static T RegisterDragSelf<T>(this T el, EventCallback<PointerMoveEvent> onDrag, Action<T, PointerDownEvent> onDown = null, Action<T, PointerMoveEvent> onBeginDrag = null, Action<T> onEnd = null) where T : VisualElement
        {
            el.RegisterCallback<PointerDownEvent>(downEvent =>
            {
                var isFirstDrag = false;
                var root = el.panel.visualTree;
                root.RegisterCallback<PointerMoveEvent>(OnMove);
                root.RegisterCallback<PointerUpEvent>(OnUp);
                root.RegisterCallback<PointerLeaveEvent>(OnLeave);
                el.RegisterCallback<DetachFromPanelEvent>(OnDetach);
                onDown?.Invoke(el, downEvent);
                return;

                void OnDetach(DetachFromPanelEvent _) => OnUp(null);
                void OnLeave(PointerLeaveEvent _) => OnUp(null);

                void OnMove(PointerMoveEvent evt)
                {
                    if (isFirstDrag)
                    {
                        isFirstDrag = false;
                        onBeginDrag?.Invoke(el, evt);
                    }
                    onDrag(evt);
                }

                void OnUp(PointerUpEvent _)
                {
                    root.UnregisterCallback<PointerMoveEvent>(OnMove);
                    root.UnregisterCallback<PointerUpEvent>(OnUp);
                    root.UnregisterCallback<PointerLeaveEvent>(OnLeave);
                    el.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
                    onEnd?.Invoke(el);
                }
            });
            return el;
        }

        public static T RegisterKeyDown<T>(this T target, Action<T> callback, KeyCode key, TrickleDown trickle = TrickleDown.TrickleDown, bool ignoreOnInputFieldFocused = true, KeyCode[] moreKeys = null) where T : VisualElement
        {
            target.focusable = true;
            target.pickingMode = PickingMode.Position;
            target.RegisterCallback<KeyDownEvent>(e =>
            {
                if (ignoreOnInputFieldFocused && target.panel?.CheckFocusInInput() == true)
                    return;
                if (e.keyCode == key)
                {
                    callback(target);
                    e.StopPropagation();
                }
                else if (moreKeys != null)
                {
                    for (var i = 0; i < moreKeys.Length; i++)
                    {
                        if (e.keyCode == moreKeys[i])
                        {
                            callback(target);
                            e.StopPropagation();
                            break;
                        }
                    }
                }
            }, trickle);
            return target;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool CheckFocusInInput(this IPanel panel)
        {
            var el = panel.focusController?.focusedElement;
            if (el == null || !el.canGrabFocus)
                return false;
            var baseType = el.GetType().BaseType;
            for (var i = 0; i < 5 && baseType != null; i++)
            {
                if (baseType.Name == "TextInputBaseField`1")
                    return true;
                if (baseType.Name == "BaseCompositeField`3")
                {
                    --i;
                    baseType = baseType.GetGenericArguments()[1].BaseType;
                }
                else
                {
                    baseType = baseType.BaseType;
                }
            }
            return false;
        }
    }
}
