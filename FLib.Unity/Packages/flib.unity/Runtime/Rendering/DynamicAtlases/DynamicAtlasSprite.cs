// ==================== qcbf@qq.com | 2025-10-30 ====================

using UnityEngine;

namespace FLib.Unity.DynamicAtlases
{
    public class DynamicAtlasSprite
    {
        public uint Id;
        public Vector2Int Position;
        public Vector2Int Size;
        public DynamicAtlas Atlas;

        public Rect Rect => new(Position.x + Atlas.PaddingHalf, Position.y + Atlas.PaddingHalf, Size.x - Atlas.Padding, Size.y - Atlas.Padding);

        public int UseCount
        {
            get;
            internal set;
        }

        public DynamicAtlasSprite Use()
        {
            ++UseCount;
            return this;
        }

        public DynamicAtlasSprite Initialize(uint id, DynamicAtlas atlas, Vector2Int position, Vector2Int size)
        {
            UseCount = 0;
            Id = id;
            Atlas = atlas;
            Position = position;
            Size = size;
            return this;
        }
    }
}
