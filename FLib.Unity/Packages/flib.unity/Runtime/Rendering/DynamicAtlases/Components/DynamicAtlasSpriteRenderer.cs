// ==================== qcbf@qq.com | 2025-10-30 ====================

using System;
using FLib;
using Unity.Collections;
using UnityEngine;

namespace FLib.Unity.DynamicAtlases
{
    public class DynamicAtlasSpriteRenderer : DynamicAtlasComponent<SpriteRenderer>
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
