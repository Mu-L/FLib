//==================={By Qcbf|qcbf@qq.com|11/26/2022 9:44:09 PM}===================

using System;
using System.Collections.Generic;
using FLib;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity
{
    public class UIFlip : MonoBehaviour, IMeshModifier
    {


        public bool IsFlipX;
        public bool IsFlipY;



        private void OnValidate()
        {
            GetComponent<Graphic>().SetVerticesDirty();
        }

        public void ModifyMesh(Mesh mesh)
        {
        }

        public void ModifyMesh(VertexHelper verts)
        {
            var r = (RectTransform)transform;
            var v = new UIVertex();
            for (var i = 0; i < verts.currentVertCount; i++)
            {
                verts.PopulateUIVertex(ref v, i);
                var p = v.position;
                if (IsFlipX)
                {
                    p.x = -p.x;
                }
                if (IsFlipY)
                {
                    p.y = -p.y;
                }
                v.position = p;
                verts.SetUIVertex(v, i);
            }

        }



    }
}
