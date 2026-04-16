// ==================== qcbf@qq.com | 2026-03-07 ====================

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    public class WorldEffectContainer : IEnumerable<WorldEffectBase>
    {
        private static readonly byte[] DeBruijn32 = { 0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9 };
        public SlimDictionary<uint, Item> Effects = new(32);
        public byte[] FlagCounts = new byte[32];

        public struct Item : IEnumerable<WorldEffectBase>
        {
            public WorldEffectBase? Single;
            public PooledList<WorldEffectBase> MoreList;

            public ItemEnumerator GetEnumerator() => new(this);
            IEnumerator<WorldEffectBase> IEnumerable<WorldEffectBase>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// 
            /// </summary>
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

            public struct ItemEnumerator : IEnumerator<WorldEffectBase>
            {
                private readonly Item _item;
                private int _index; // -1 = before Single, 0+ = MoreList index
                public WorldEffectBase Current { get; private set; }
                object IEnumerator.Current => Current;

                public ItemEnumerator(Item item)
                {
                    _item = item;
                    _index = -2; // not started
                    Current = null!;
                }

                public bool MoveNext()
                {
                    if (_item.Single == null) return false;
                    if (_index == -2) // first call
                    {
                        _index = -1;
                        Current = _item.Single;
                        return true;
                    }

                    _index++;
                    if (_index < _item.MoreList.Count)
                    {
                        Current = _item.MoreList[_index];
                        return true;
                    }

                    return false;
                }

                public void Reset() => _index = -2;

                public void Dispose()
                {
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddFlags(uint flags)
        {
            while (flags != 0)
            {
                FlagCounts[TrailingZeros(flags)]++;
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
                if (--FlagCounts[bit] == 0)
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
            Array.Clear(FlagCounts, 0, FlagCounts.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        private static int TrailingZeros(uint v) => DeBruijn32[(v & (uint)-(int)v) * 0x077CB531u >> 27];

        public Enumerator GetEnumerator() => new(Effects);
        IEnumerator<WorldEffectBase> IEnumerable<WorldEffectBase>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator<WorldEffectBase>
        {
            private SlimDictionary<uint, Item>.Enumerator _enumerator;
            private Item.ItemEnumerator _enumerator2;
            public WorldEffectBase Current => _enumerator2.Current;
            object IEnumerator.Current => Current;

            internal Enumerator(SlimDictionary<uint, Item> effects)
            {
                _enumerator = effects.GetEnumerator();
                _enumerator2 = default;
            }


            public bool MoveNext()
            {
                while (!_enumerator2.MoveNext())
                {
                    if (!_enumerator.MoveNext())
                        return false;
                    _enumerator2 = _enumerator.Value.GetEnumerator();
                }

                return true;
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
            }
        }
    }
}