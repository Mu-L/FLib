// =================================================={By Qcbf|qcbf@qq.com|2024-2-19}==================================================
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Rendering;

namespace FLib.Unity
{
    public class EditorDrawUtility
    {
        public static Material MapTileMaterial;

        public static Color TileColor
        {
            set => GL.Color(value);
        }

        public static void TryInitialize()
        {
            if (MapTileMaterial == null)
            {
                MapTileMaterial = new(Shader.Find($"Hidden/Internal-Colored"));
                MapTileMaterial.SetFloat($"_ZTest", (int)CompareFunction.Always);
            }

            MapTileMaterial.SetPass(0);
        }

        public static void GLDrawQuadLines(Vector2 min, Vector2 max, float y = 0)
        {
            GL.Vertex3(min.x, y, min.y);
            GL.Vertex3(max.x, y, min.y);

            GL.Vertex3(max.x, y, min.y);
            GL.Vertex3(max.x, y, max.y);

            GL.Vertex3(max.x, y, max.y);
            GL.Vertex3(min.x, y, max.y);

            GL.Vertex3(min.x, y, max.y);
            GL.Vertex3(min.x, y, min.y);
        }
    }
}

#endif