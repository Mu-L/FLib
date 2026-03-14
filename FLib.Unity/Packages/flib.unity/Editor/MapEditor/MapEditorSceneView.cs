// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using UnityEditor;
using UnityEngine;

namespace FLib.Unity.Editor
{
    public class MapEditorSceneView
    {
        public bool? BrushState;
        public int BrushSize = 1;
        public MapEditorInspector MapEditor;
        public EBrushType BrushType;

        public enum EBrushType
        {
            Auto,
            Set,
            Unset,
        }

        public MapEditorSceneView(MapEditorInspector mapEditor)
        {
            MapEditor = mapEditor;
        }

        public void OnSceneGUI()
        {
            var map = MapEditor.Map;
            if (map == null || map.LayerCount == 0)
                return;
            EditorDrawQuadMap.DrawTiles(0, map);

            var e = Event.current;
            FVector2Int mapPos = default;
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var mouseCamereDist))
            {
                var worldPos = ray.GetPoint(mouseCamereDist);
                mapPos = map.WorldToMapPos(worldPos.AsFVec2XZ());
            }
            DrawGUI(mapPos);
            DrawScene(mapPos, map);
        }

        private void DrawGUI(in FVector2Int mapPos)
        {
            Handles.BeginGUI();
            var rect = EditorGUILayout.BeginHorizontal();
            GUI.Box(rect, "");
            GUILayout.Label($"{mapPos} Size:{BrushSize}", GUILayout.ExpandWidth(false));
            BrushType = (EBrushType)EditorGUILayout.EnumPopup(BrushType);
            EditorGUILayout.EndHorizontal();
            Handles.EndGUI();
        }

        private void DrawScene(in FVector2Int mapPos, QuadMap map)
        {
            var e = Event.current;
            var tileSizeHalf = map.TileSize * 0.5f;
            Handles.DrawWireCube(new Vector3(mapPos.X + tileSizeHalf + map.Offset.X, 0, mapPos.Y + tileSizeHalf + map.Offset.Y), new Vector3(BrushSize, 0, BrushSize));

            if (e.button == 0)
            {
                if (e.type == EventType.MouseDown)
                {
                    e.Use();
                    if (map.CheckTile(mapPos))
                    {
                        MapEditor.IsChanged = true;
                        BrushState = BrushType == EBrushType.Auto ? !map[MapEditor.EditLayer, mapPos] : BrushType == EBrushType.Set;
                        Brush(mapPos);
                    }
                }
                if (BrushState != null)
                {
                    if (e.type == EventType.MouseDrag)
                    {
                        e.Use();
                        if (map.CheckTile(mapPos))
                            Brush(mapPos);
                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        BrushState = null;
                        e.Use();
                    }
                }
            }
            if (e.type == EventType.KeyDown)
            {
                if (e.control && e.keyCode == KeyCode.S)
                {
                    MapEditor.WriteMap();
                    Log.Info?.Write($"save map");
                }
                else if (e.keyCode == KeyCode.LeftBracket)
                {
                    BrushSize = Mathf.Clamp(BrushSize - 2, 1, 100);
                }
                else if (e.keyCode == KeyCode.RightBracket)
                {
                    BrushSize = Mathf.Clamp(BrushSize + 2, 1, 100);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void Brush(FVector2Int mapPos)
        {
            var sizeHalf = (int)(BrushSize * 0.5f);
            for (var x = mapPos.X - sizeHalf; x <= mapPos.X + sizeHalf; x++)
            {
                for (var y = mapPos.Y - sizeHalf; y <= mapPos.Y + sizeHalf; y++)
                {
                    var pos = new FVector2Int(x, y);
                    if (MapEditor.Map.CheckTile(pos))
                        MapEditor.Map[MapEditor.EditLayer, pos] = BrushState!.Value;
                }
            }
        }
    }
}
