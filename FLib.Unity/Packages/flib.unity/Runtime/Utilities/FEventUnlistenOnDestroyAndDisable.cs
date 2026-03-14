// ==================== qcbf@qq.com | 2025-07-25 ====================

using System;
using System.Collections.Generic;
using FLib;
using UnityEngine;

namespace FLib.Unity.Utilities
{
    public class FEventUnlistenOnDestroyAndDisable : MonoBehaviour
    {
        public FEventListenManaged OnDestoryManaged;
        public FEventListenManaged OnDisableManaged;

        /// <summary>
        /// 
        /// </summary>
        private void OnDestroy()
        {
            OnDestoryManaged.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnDisable()
        {
            OnDisableManaged.Dispose();
        }
    }
}
