// ==================== qcbf@qq.com | 2025-09-15 ====================

using System;
using Cysharp.Threading.Tasks;
using FLib;
using FLib.Unity;
using Modules;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Utilities
{
    public static class UIUtility
    {
        /// <summary>
        /// 
        /// </summary>
        public static void InstantiateTemplate<T>(T item, int count, Action<int, T> initializeItemHandler) where T : MonoBehaviour
            => InstantiateTemplateAsync(item, count, initializeItemHandler).Forget();

        /// <summary>
        /// 
        /// </summary>
        public static async UniTask InstantiateTemplateAsync<T>(T item, int count, Action<int, T> initializeItemHandler) where T : MonoBehaviour
        {
            var root = item.transform.parent;
            root.ClearChildren(ignore: item.transform);
            if (count <= 0)
            {
                item.gameObject.SetActive(false);
                return;
            }
            item.gameObject.SetActive(true);
            initializeItemHandler(0, item);
            if (count > 1)
            {
                var items = await Object.InstantiateAsync(item, count - 1, root);
                for (var i = 1; i < count; i++)
                {
                    if (i % 100 == 0)
                        await UniTask.Yield();
                    initializeItemHandler(i, items[i - 1]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static async UniTask UICameraOnly(int delay, Func<bool> conditionHandler)
        {
            var condition = conditionHandler();
            await UniTask.Delay(delay);
            if (conditionHandler() != condition) return;
            var worldCam = CinemachineBrain.GetActiveBrain(0);
            var uiCam = UIRoot.UICamera;
            if (condition)
            {
                worldCam.OutputCamera.enabled = false;
                uiCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
                uiCam.clearFlags = CameraClearFlags.SolidColor;
                uiCam.backgroundColor = Color.clear;
            }
            else
            {
                worldCam.OutputCamera.enabled = true;
                uiCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            }
        }
    }
}
