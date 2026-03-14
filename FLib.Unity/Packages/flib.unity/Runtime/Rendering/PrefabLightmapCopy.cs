//=================================================={By Qcbf|qcbf@qq.com|11/30/2024 8:42:45 PM}==================================================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FLib.Unity
{
    [ExecuteAlways]
    public class PrefabLightmapCopy : MonoBehaviour
    {
        public MeshRenderer Renderer;
        public int LightmapIndex = -1;

        private void OnEnable()
        {
            if (Renderer == null)
                Renderer = GetComponent<MeshRenderer>();

            if (Renderer.lightmapIndex == -1)
            {
                ApplyLightmap();
            }
        }


        [MethodButton]
        public void ApplyLightmap()
        {
            Renderer.lightmapIndex = LightmapIndex;
        }


        [MethodButton]
        public void CopyLightmap()
        {
            var r = GetComponent<MeshRenderer>();
            LightmapIndex = r.lightmapIndex;
        }

#if UNITY_EDITOR
        [MethodButton]
        internal void CopyAllLightmaps()
        {
            foreach (var r in transform.root.GetComponentsInChildren<PrefabLightmapCopy>(true))
                r.CopyLightmap();
        }
#endif
    }
}
