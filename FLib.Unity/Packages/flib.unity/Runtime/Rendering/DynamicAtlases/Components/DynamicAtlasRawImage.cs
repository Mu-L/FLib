// ==================== qcbf@qq.com | 2025-10-31 ====================

using FLib;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity.DynamicAtlases
{
    public class DynamicAtlasRawImage : DynamicAtlasComponent<RawImage>
    {

        public Texture texture
        {
            get => Target.texture;
            set
            {
                Target.texture = value;
                Refresh();
            }
        }
        
        public override void ConvertToDynamicAtlas()
        {
            if (Target.texture == null)
                return;
            var dSprite = MakeDynamicSprite((Texture2D)Target.texture);
            Target.texture = dSprite.Atlas.AtlasTexture;
            Target.uvRect = dSprite.Rect;
        }
    }
}
