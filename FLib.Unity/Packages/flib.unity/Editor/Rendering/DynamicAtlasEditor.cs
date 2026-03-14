// ==================== qcbf@qq.com | 2025-10-31 ====================

using FLib;
using FLib.Unity.DynamicAtlases;
using UnityEditor;
using UnityEngine;

namespace FLib.Unity.Editor.DynamicAtlases
{
    [CustomEditor(typeof(DynamicAtlas))]
    public class DynamicAtlasEditor : UnityEditor.Editor
    {
        private GUIContent _title = new();

        public override bool HasPreviewGUI() => true;

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var atlas = (DynamicAtlas)target;
            if (atlas.AtlasTexture != null)
                GUI.DrawTexture(r, atlas.AtlasTexture);
        }

        public override GUIContent GetPreviewTitle()
        {
            var atlas = (DynamicAtlas)target;
            _title.text =  atlas.DynamicSprites.Count.ToString();
            return _title;
        }
    }
}
