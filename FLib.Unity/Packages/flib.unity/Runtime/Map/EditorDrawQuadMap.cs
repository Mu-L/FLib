// =================================================={By Qcbf|qcbf@qq.com|2024-1-21}==================================================

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace FLib.Unity
{
    public static class EditorDrawQuadMap
    {
        /// <summary>
        /// 
        /// </summary>
        public static void DrawTiles(byte layer, QuadMap map, Action<QuadMap, int, FVector2> glDrawHandle = null)
        {
            EditorDrawUtility.TryInitialize();
            var terrain = map.Terrains[layer];
            var tileSize = (float)map.TileSize;
            var tileSizeHalf = tileSize * 0.5f;
            GL.PushMatrix();
            GL.MultMatrix(UnityEditor.Handles.matrix);
            GL.Begin(GL.QUADS);
            for (var i = 0; i < terrain.Length; i++)
            {
                var worldPos = map.MapToWorldPos(map.IdxToPos(i)).AsVec();
                worldPos.x -= tileSizeHalf;
                worldPos.y -= tileSizeHalf;
                if (glDrawHandle == null)
                    EditorDrawUtility.TileColor = terrain[i] ? new Color(1, 0, 0, 0.5f) : new Color(1, 1, 1, 0.5f);
                else
                    glDrawHandle.Invoke(map, i, worldPos.AsFVec2());
                GL.Vertex3(worldPos.x, 0, worldPos.y);
                GL.Vertex3(worldPos.x + tileSize, 0, worldPos.y);
                GL.Vertex3(worldPos.x + tileSize, 0, worldPos.y + tileSize);
                GL.Vertex3(worldPos.x, 0, worldPos.y + tileSize);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}

#endif
