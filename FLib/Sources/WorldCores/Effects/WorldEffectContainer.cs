// ==================== qcbf@qq.com | 2026-03-07 ====================

using System;
using System.Collections.Generic;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    public class WorldEffectContainer
    {
        public SlimDictionary<uint, Item> Effects = new(32);
        public SlimDictionary<BitFlags, int> FlagsCount = new(8);

        public struct Item
        {
            public WorldEffect Single;
            public PooledList<WorldEffect> MoreList;
        }
    }
}