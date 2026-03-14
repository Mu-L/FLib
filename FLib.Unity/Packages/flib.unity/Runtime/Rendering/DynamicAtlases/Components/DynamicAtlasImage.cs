// ==================== qcbf@qq.com | 2025-10-31 ====================

using FLib;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity.DynamicAtlases
{
    public class DynamicAtlasImage : DynamicAtlasComponent<Image>
    {
        public Sprite sprite
        {
            get => Target.sprite;
            set
            {
                Target.sprite = value;
                Refresh();
            }
        }

        public override void ConvertToDynamicAtlas()
        {
            var uSprite = Target.sprite;
            if (uSprite == null)
                return;
            Target.sprite = CreateUnitySprite(uSprite);
        }
    }
}
