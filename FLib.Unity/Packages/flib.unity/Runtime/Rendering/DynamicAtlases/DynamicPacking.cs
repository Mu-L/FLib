using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FLib.Unity.DynamicAtlases
{
    public abstract class DynamicPacking
    {
        private Vector2Int _size;
        private int _padding;

        public Vector2Int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _size;
        }

        public int Padding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _padding;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _padding = value;
        }

        public int TotalArea
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _size.x * _size.y;
        }

        public int FillArea
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TotalArea;
        }

        public float FillRate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (float)FillArea / (float)TotalArea;
        }

        protected DynamicPacking()
        {
        }

        protected DynamicPacking(Vector2Int size, int padding)
        {
            _size = size;
            _padding = padding;
        }

        public bool AddImage(Vector2Int size, out Vector2Int pos, uint guid)
        {
            pos = default;

            if (size.x <= 0 || size.y <= 0)
                return false;

            int width = size.x;
            int height = size.y;

            if (_padding > 0)
            {
                int spaceX = Mathf.Clamp((_size.x - size.x) / 2, 0, _padding);
                int spaceY = Mathf.Clamp((_size.y - size.y) / 2, 0, _padding);

                width += spaceX * 2;
                height += spaceY * 2;
            }

            if (!OnAddImage(guid, width, height, out pos))
                return false;

            pos.x += _padding;
            pos.y += _padding;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FreeImage(uint imageId) => OnFreeImage(imageId);

        public virtual void ClearAllImages()
        {
        }

        protected abstract bool OnAddImage(uint imageId, int width, int height, out Vector2Int pos);
        protected abstract bool OnFreeImage(uint imageId);
    }
}
