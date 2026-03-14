// ==================== qcbf@qq.com | 2025-10-30 ====================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace FLib.Unity.DynamicAtlases
{
    [Comment("动态图集")]
    public class DynamicAtlas : ScriptableObject
    {
        [Header("图集总大小\n超出会报错")] public Vector2Int Size = new(2048, 2048);

        [Header("空隙像素\n出图时也需要手动增加这个尺寸，每边增加Padding/2")]
        public int Padding = 4;

        [Header("图集格式\n必须和散图保持格式一致\n如果是astc，尺寸必须是astc的整倍数\n如astc6，所有数字必须能被6整除，尺寸，padding都是")]
        public TextureFormat Format = TextureFormat.ASTC_6x6;

        public FilterMode FilterMode = FilterMode.Bilinear;
        public DynamicPacking Packing;
        [NonSerialized] public Texture2D AtlasTexture;
        public Dictionary<uint, DynamicAtlasSprite> DynamicSprites;

        private CopyTextureSupport _copyTextureSupport;
        public int PaddingHalf { get; private set; }
        public bool IsAstc => Format >= TextureFormat.ASTC_4x4 && Format <= TextureFormat.ASTC_12x12;

        public int AstcBlockSize => Format switch
        {
            TextureFormat.ASTC_4x4 => 4,
            TextureFormat.ASTC_5x5 => 5,
            TextureFormat.ASTC_6x6 => 6,
            TextureFormat.ASTC_8x8 => 8,
            TextureFormat.ASTC_10x10 => 10,
            TextureFormat.ASTC_12x12 => 12,
            _ => 0
        };

        private void OnEnable()
        {
            DynamicSprites = new Dictionary<uint, DynamicAtlasSprite>(Mathf.Max(8, Size.x / 4));
            PaddingHalf = Padding >> 1;
            _copyTextureSupport = SystemInfo.copyTextureSupport;
            Packing = new DynamicPackingBinaryTree(Size, 0);
            AtlasTexture = new Texture2D(Size.x, Size.y, Format, false) { hideFlags = HideFlags.DontSave, filterMode = FilterMode, wrapMode = TextureWrapMode.Clamp };
            if (_copyTextureSupport != CopyTextureSupport.None && IsAstc)
                AtlasTexture.Apply(false, true);
        }

        private void OnDisable()
        {
            if (AtlasTexture == null) return;
            UnityFLibUtility.Destroy(AtlasTexture);
        }

        /// <summary>
        /// 添加纹理图块到动态图集
        /// </summary>
        public DynamicAtlasSprite AddSprite(Texture2D spriteTexture)
        {
            return AddSprite((uint)spriteTexture.GetInstanceID(), spriteTexture);
        }

        /// <summary>
        /// 添加纹理图块到动态图集
        /// </summary>
        public DynamicAtlasSprite AddSprite(uint id, Texture2D spriteTexture)
        {
            Log.Assert(id > 0);
            if (DynamicSprites.TryGetValue(id, out var dSprite))
                return dSprite.Use();

            var uSpriteSize = new Vector2Int(spriteTexture.width, spriteTexture.height);
#if UNITY_EDITOR
            if (IsAstc)
            {
                var blockSize = AstcBlockSize;
                if (uSpriteSize.x % blockSize != 0 || uSpriteSize.y % blockSize != 0)
                    throw new Exception($"size must be multiple of {blockSize}, {spriteTexture}");
            }

            if (spriteTexture.format != Format)
                throw new Exception($"sprite texture format error: {spriteTexture} {spriteTexture.format}, need {Format}");
#endif
            if (!Packing.AddImage(uSpriteSize, out var spritePos, id))
                throw new Exception($"dynamic atlas overflow {name} {spriteTexture} {uSpriteSize}");

            if (_copyTextureSupport != CopyTextureSupport.None)
            {
                Graphics.CopyTexture(spriteTexture, 0, 0, 0, 0, uSpriteSize.x, uSpriteSize.y, AtlasTexture, 0, 0, spritePos.x, spritePos.y);
            }
            else
            {
                AtlasTexture.SetPixels32(spritePos.x, spritePos.y, uSpriteSize.x, uSpriteSize.y, spriteTexture.GetPixels32(), 0);
                AtlasTexture.Apply(false);
            }

            DynamicSprites.Add(id, dSprite = GlobalObjectPool<DynamicAtlasSprite>.Create().Initialize(id, this, spritePos, uSpriteSize));
            return dSprite.Use();
        }

        /// <summary>
        /// 从动态图集中移除指定ID的精灵
        /// </summary>
        /// <param name="id">要移除的精灵的ID</param>
        /// <returns>如果成功移除精灵则返回true，否则返回false</returns>
        public bool RemoveSprite(uint id)
        {
            if (DynamicSprites.TryGetValue(id, out var dSprite) && --dSprite.UseCount <= 0)
            {
                GlobalObjectPool<DynamicAtlasSprite>.Release(dSprite);
                DynamicSprites.Remove(id);
                return Packing.FreeImage(id);
            }

            return false;
        }
    }
}