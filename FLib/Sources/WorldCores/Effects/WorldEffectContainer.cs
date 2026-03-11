// ==================== qcbf@qq.com | 2026-03-07 ====================

#nullable enable
using System;
using System.Collections.Generic;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    public class WorldEffectContainer : IWorldUpdate
    {
        private static readonly byte[] DeBruijn32 = { 0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9 };
        public SlimDictionary<uint, Item> Effects = new(32);
        public byte[] FlagsCount = new byte[32];

        public struct Item
        {
            public WorldEffect? Single;
            public PooledList<WorldEffect> MoreList;

            public bool TryPopMoreList()
            {
                if (MoreList.IsEmpty)
                    return false;
                var index = MoreList.Count - 1;
                Single = MoreList[index];
                MoreList.RemoveAt(index);
                if (MoreList.IsEmpty)
                    MoreList.Dispose();
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Update(WorldCore world, WorldEntity entity)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddFlags(uint flags)
        {
            while (flags != 0)
            {
                FlagsCount[TrailingZeros(flags)]++;
                flags &= flags - 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint RemoveFlags(uint flags)
        {
            var clearMask = 0U;
            while (flags != 0)
            {
                var bit = TrailingZeros(flags);
                if (--FlagsCount[bit] == 0)
                    clearMask |= 1U << bit;
                flags &= flags - 1;
            }

            return clearMask;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            Array.Clear(FlagsCount);
        }

        /// <summary>
        /// 
        /// </summary>
        private static int TrailingZeros(uint v) => DeBruijn32[(v & (uint)-(int)v) * 0x077CB531u >> 27];
    }
}