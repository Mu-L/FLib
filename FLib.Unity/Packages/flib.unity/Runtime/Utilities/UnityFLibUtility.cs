//==================={By Qcbf|qcbf@qq.com|9/14/2021 6:54:00 PM}===================

using FLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace FLib.Unity
{
    public static class UnityFLibUtility
    {
        /// <summary>
        /// 
        /// </summary>
        public static void ClearChildren(this Transform transf, int skip = 0, Transform ignore = null)
        {
            for (var i = transf.childCount - 1; i >= skip; i--)
            {
                var item = transf.GetChild(i);
                if (item == transf || item == null || ignore == item) continue;
                item.SetParent(null);
                Destroy(item.gameObject);
            }
        }

        /// <summary>
        /// 销毁目标，不用在意编辑器或者运行时区分DestroyImmediate
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target, true);
                return;
            }
#endif
            Object.Destroy(target);
        }

        /// <summary>
        /// 尝试销毁目标，并且设置为null
        /// </summary>
        public static void TryDestroy<T>(ref T target, bool isWithGameObject = true) where T : Object
        {
            if (target == null) return;
            if (target is Transform tf)
                Destroy(tf.gameObject);
            else if (isWithGameObject && target is Component comp)
                Destroy(comp.gameObject);
            else
                Destroy(target);
            target = null;
        }

        /// <summary>
        /// 得到透视摄像机屏幕尺寸
        /// </summary>
        public static Vector2 GetPerspectiveCameraHalfSize(this Camera cam, float dist)
        {
            return GetPerspectiveCameraHalfSize(cam.fieldOfView, cam.aspect, dist);
        }

        /// <summary>
        /// 得到透视摄像机屏幕尺寸
        /// </summary>
        public static Vector2 GetPerspectiveCameraHalfSize(float fov, float aspect, float dist)
        {
            var size = new Vector2(0, dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
            size.x = size.y * aspect;
            return size;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerator Coroutine(object yield, Action handler)
        {
            yield return yield;
            handler();
        }

        /// <summary>
        /// 简单的tween动画
        /// </summary>
        public static IEnumerator Tween<T>(object target, T from, T to, float duration, Action<object, float, T, T> update, FTweenAnimation.EEaseType easeType = FTweenAnimation.EEaseType.Linear)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                update(target, FTweenAnimation.Tween(easeType, (FNum)(elapsed / duration)), from, to);
                yield return 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsPointerOverUIObject()
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                var pos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
                using var listPool = ListPool<RaycastResult>.Get(out var list);
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { pressPosition = pos, position = pos }, list);
                return list.Exists(v => v.gameObject.layer == UIRoot.Inst.gameObject.layer);
            }
            if (EventSystem.current.IsPointerOverGameObject())
                return true;
            if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return false;
        }

        /// <summary>
        /// 计算分辨率宽度
        /// </summary>
        public static int CalcResolutionWidth(float x = 720)
        {
            return (int)(x * (Screen.width / (float)Screen.height));
        }

        /// <summary>
        /// 获取transform路径
        /// </summary>
        public static string GetTransformPath(this Transform target, Transform endToTarget = null, int skipRootCount = 0, char splitChar = '/', Stack<string> names = null)
        {
            if (target == null) return string.Empty;
            (names ??= new Stack<string>(target.childCount * 8)).Clear();
            while (target != null && target != endToTarget)
            {
                names.Push(target.name);
                target = target.parent;
            }

            while (skipRootCount-- > 0) names.Pop();
            return string.Join(splitChar, names);
        }

        /// <summary>
        ///
        /// </summary>
        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                child.gameObject.SetLayerRecursively(layer);
        }

        /// <summary>
        /// 
        /// </summary>
        public static T GetComponent<T>(this Object obj) where T : Component
        {
            if (obj is GameObject gameObj)
                return gameObj.GetComponent<T>();
            if (obj is Component comp)
                return comp.GetComponent<T>();
            throw new NotSupportedException(typeof(T).ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        public static T AddComponent<T>(this Object obj) where T : Component
        {
            if (obj is GameObject gameObj)
                return gameObj.AddComponent<T>();
            if (obj is Component comp)
                return comp.gameObject.AddComponent<T>();
            throw new NotSupportedException(typeof(T).ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        public static T GetOrAddComponent<T>(this Object uo) where T : Component
        {
            return uo.GetComponent<T>() ?? uo.AddComponent<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObj) where T : Component
        {
            return gameObj.GetComponent<T>() ?? gameObj.AddComponent<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        public static Component GetComponent(this Object obj, Type type)
        {
            if (obj is GameObject gameObj)
                return gameObj.GetComponent(type);
            if (obj is Component comp)
                return comp.GetComponent(type);
            throw new NotSupportedException(type.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        
        public static Component AddComponent(this Object obj, Type type)
        {
            if (obj is GameObject gameObj)
                return gameObj.AddComponent(type);
            if (obj is Component comp)
                return comp.gameObject.AddComponent(type);
            throw new NotSupportedException(type.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        public static Component GetOrAddComponent(this Object uo, Type type)
        {
            return uo.GetComponent(type) ?? uo.AddComponent(type);
        }

        /// <summary>
        /// 
        /// </summary>
        public static Component GetOrAddComponent(this GameObject gameObj, Type type)
        {
            return gameObj.GetComponent(type) ?? gameObj.AddComponent(type);
        }

        /// <summary>
        /// 
        /// </summary>
        public static float3 SmoothDamp(float3 current, float3 target, float smoothTime)
        {
            var delta = Time.deltaTime;
            var alpha = 1f - math.exp(-delta / smoothTime);
            current = math.lerp(current, target, alpha);
            return current;
        }

        /// <summary>
        /// 
        /// </summary>
        public static float3 SmoothDamp(float3 current, float3 target, ref float3 velocity, float smoothTime)
        {
            var delta = Time.deltaTime;
            var omega = 2f / math.max(0.0001f, smoothTime);
            var x = omega * delta;
            var exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            var change = current - target;
            var temp = (velocity + omega * change) * delta;
            velocity = (velocity - omega * temp) * exp;
            var output = target + (change + temp) * exp;
            if (math.dot(target - current, output - target) > 0f)
            {
                output = target;
                velocity = (output - target) / delta;
            }
            return output;
        }
    }
}
