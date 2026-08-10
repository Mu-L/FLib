// ==================={By Qcbf|qcbf@qq.com|10/14/2022 10:56:47 AM}===================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FLib
{
    /// <summary> 基于分层位图的二维格子地图。 </summary>
    public class QuadMap : IBytesSerializable
    {
        /// <summary> 按顺时针排列的八方向邻近格偏移。 </summary>
        private static readonly FVector2Int[] NearestEightPositions = { new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1) };

        /// <summary> 地图左下角的世界坐标。 </summary>
        public FVector2 Offset;

        /// <summary> 单个格子的世界尺寸。 </summary>
        public FNum TileSize = FNum.One;

        /// <summary> 地图的格子尺寸。 </summary>
        public FVector2Int TerrainSize;

        /// <summary> 按层存储的格子位图数据。 </summary>
        public ulong[][] Terrain;

        /// <summary> 地图层数。 </summary>
        public int LayerCount => Terrain?.Length ?? 0;

        /// <summary> 地图在世界空间中的尺寸。 </summary>
        public FVector2 WorldSize => new(TerrainSize.X * TileSize, TerrainSize.Y * TileSize);

        /// <summary> 地图在世界空间中的边界矩形。 </summary>
        public FRect WorldRect => new(Offset, Offset + WorldSize);

        /// <summary> 获取或设置指定层和格子坐标的值。 </summary>
        public bool this[int layer, in FVector2Int pos]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => this[layer, PosToIdx(pos)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => this[layer, PosToIdx(pos)] = value;
        }

        /// <summary> 获取或设置指定层及横纵坐标的值。 </summary>
        public bool this[int layer, in int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => this[layer, y * TerrainSize.X + x];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => this[layer, y * TerrainSize.X + x] = value;
        }

        /// <summary> 获取或设置指定层和线性索引的值。 </summary>
        public bool this[int layer, int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => BitArrayOperator.GetBit(Terrain[layer], index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => BitArrayOperator.SetBit(Terrain[layer], index, value);
        }

        /// <summary> 将地图配置和地形数据深拷贝到目标地图。 </summary>
        public virtual void CopyTo(QuadMap to)
        {
            if (to == null) return;
            to.Offset = Offset;
            to.TileSize = TileSize;
            to.TerrainSize = TerrainSize;
            to.Terrain = new ulong[LayerCount][];
            for (var i = 0; i < LayerCount; i++)
                to.Terrain[i] = (ulong[])Terrain[i].Clone();
        }

        /// <summary> 将格子坐标转换为格子中心的世界坐标。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector2 MapToWorldPos(in FVector2Int pos)
        {
            var half = TileSize * FNum.OneHalf;
            return new FVector2(pos.X * TileSize + half + Offset.X, pos.Y * TileSize + half + Offset.Y);
        }

        /// <summary> 将世界坐标转换为格子坐标。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector2Int WorldToMapPos(FVector2 pos) => new((int)FNum.Floor((pos.X - Offset.X) / TileSize), (int)FNum.Floor((pos.Y - Offset.Y) / TileSize));

        /// <summary> 将线性索引转换为格子坐标。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector2Int IdxToPos(int index) => new() { X = index % TerrainSize.X, Y = index / TerrainSize.X };

        /// <summary> 将格子坐标转换为线性索引。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PosToIdx(in FVector2Int pos) => pos.Y * TerrainSize.X + pos.X;

        /// <summary> 设置层数并保留各层已有位数据。 </summary>
        public virtual QuadMap SetLayers(int count)
        {
            if (LayerCount == count)
                return this;
            Array.Resize(ref Terrain, count);
            var tileCount = BitArrayOperator.GetBitsLength(TerrainSize.X * TerrainSize.Y);
            for (var i = 0; i < count; i++)
            {
                if (Terrain[i] == null || Terrain[i].Length != tileCount)
                    Array.Resize(ref Terrain[i], tileCount);
            }
            return this;
        }

        /// <summary> 设置地图尺寸和可选层数，并保留交集区域的位数据。 </summary>
        public virtual QuadMap SetSize(in FVector2Int size, int layerCount = -1)
        {
            if (size.X < 0 || size.Y < 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            var oldSize = TerrainSize;
            var oldLayerCount = LayerCount;
            var newLayerCount = layerCount > 0 ? layerCount : Math.Max(oldLayerCount, 1);
            if (oldSize == size && oldLayerCount == newLayerCount)
                return this;

            var copyWidth = Math.Min(oldSize.X, size.X);
            var copyHeight = Math.Min(oldSize.Y, size.Y);
            var tileCount = checked(size.X * size.Y);
            Array.Resize(ref Terrain, newLayerCount);
            TerrainSize = size;
            var wordCount = BitArrayOperator.GetBitsLength(tileCount);
            for (var i = 0; i < newLayerCount; i++)
            {
                var oldTerrain = Terrain[i];
                var newTerrain = new ulong[wordCount];
                if (oldTerrain != null && copyWidth > 0)
                    for (var y = 0; y < copyHeight; y++)
                        CopyBits(oldTerrain, y * oldSize.X, newTerrain, y * size.X, copyWidth);

                Terrain[i] = newTerrain;
            }

            return this;
        }

        /// <summary> 检查格子坐标是否位于地图边界内。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckTileValid(in FVector2Int pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < TerrainSize.X && pos.Y < TerrainSize.Y;

        /// <summary> 检查指定层的格子是否位于地图内且具有目标值。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckTile(in FVector2Int pos, bool value = false, int layer = 0) => CheckTileValid(pos) && BitArrayOperator.GetBit(Terrain[layer], PosToIdx(pos)) == value;

        /// <summary> 检查指定层的矩形区域是否位于地图内且全部具有目标值。 </summary>
        public bool CheckTile(in FVector2Int pos, FVector2Int size, bool value = false, int layer = 0)
        {
            if (size == FVector2Int.One)
                return CheckTile(pos, value, layer);
            if (size.X <= 0 || size.Y <= 0 || !CheckTileValid(pos) || size.X > TerrainSize.X - pos.X || size.Y > TerrainSize.Y - pos.Y)
                return false;

            var terrain = Terrain[layer];
            for (var y = 0; y < size.Y; y++)
            {
                var index = (pos.Y + y) * TerrainSize.X + pos.X;
                var remaining = size.X;
                while (remaining > 0)
                {
                    var count = Math.Min(remaining, BitArrayOperator.BitSize);
                    var mask = GetBitMask(count);
                    var bits = ReadBits(terrain, index, count);
                    if (bits != (value ? mask : 0))
                        return false;
                    index += count;
                    remaining -= count;
                }
            }

            return true;
        }

        /// <summary> 在指定层查找邻近的单格目标位置。 </summary>
        public bool TryFindNearPos(FVector2Int pos, out FVector2Int o, int findMaxDist = 0, bool value = false, Func<QuadMap, FVector2Int, bool> checker = null, int layer = 0) => TryFindNearPos(pos, FVector2Int.One, out o, findMaxDist, value, checker, layer);

        /// <summary> 在指定层查找邻近的矩形目标位置。 </summary>
        public bool TryFindNearPos(FVector2Int pos, FVector2Int size, out FVector2Int o, int findMaxDist = 0, bool value = false, Func<QuadMap, FVector2Int, bool> checker = null, int layer = 0)
        {
            o = FindNearPos(pos, size, findMaxDist, value, checker, layer);
            return o != FVector2Int.None;
        }

        /// <summary> 在指定层查找邻近的单格目标位置，失败时返回 None。 </summary>
        public FVector2Int FindNearPos(FVector2Int pos, int findMaxDist = 0, bool value = false, Func<QuadMap, FVector2Int, bool> checker = null, int layer = 0) => FindNearPos(pos, FVector2Int.One, findMaxDist, value, checker, layer);

        /// <summary> 在指定层查找邻近的矩形目标位置，失败时返回 None。 </summary>
        public FVector2Int FindNearPos(FVector2Int pos, FVector2Int size, int findMaxDist = 0, bool value = false, Func<QuadMap, FVector2Int, bool> checker = null, int layer = 0)
        {
            if (CheckPosition(pos, size, value, checker, layer))
                return pos;

            var maxDistance = findMaxDist > 0 ? findMaxDist : Math.Max(TerrainSize.X, TerrainSize.Y) - 1;
            for (var distance = 1; distance <= maxDistance; distance++)
            {
                var left = pos.X - distance;
                var right = pos.X + distance;
                var bottom = pos.Y - distance;
                var top = pos.Y + distance;
                for (var x = left; x <= right; x++)
                {
                    var mapPos = new FVector2Int(x, bottom);
                    if (CheckPosition(mapPos, size, value, checker, layer))
                        return mapPos;
                    mapPos.Y = top;
                    if (CheckPosition(mapPos, size, value, checker, layer))
                        return mapPos;
                }

                for (var y = bottom + 1; y < top; y++)
                {
                    var mapPos = new FVector2Int(left, y);
                    if (CheckPosition(mapPos, size, value, checker, layer))
                        return mapPos;
                    mapPos.X = right;
                    if (CheckPosition(mapPos, size, value, checker, layer))
                        return mapPos;
                }
            }

            return FVector2Int.None;
        }

        /// <summary> 沿目标方向查找指定层的下一步可用格子。 </summary>
        public FVector2Int FindNearNextStepPos(in FVector2Int from, in FVector2Int to, bool value = false, Func<QuadMap, FVector2Int, bool> checker = null, HashSet<FVector2Int> blackPositions = null, int layer = 0)
        {
            var directionIndex = GetNearestDirectionIndex(from, to);
            for (var i = 0; i < NearestEightPositions.Length; i++)
            {
                var offset = i == 0 ? 0 : ((i + 1) >> 1) * ((i & 1) == 0 ? -1 : 1);
                var next = from + NearestEightPositions[(directionIndex + offset) & 7];
                if (CheckPosition(next, value, checker, blackPositions, layer))
                    return next;
            }

            return FVector2Int.None;
        }

        /// <summary> 将世界坐标限制到地图内，并回退到指定层的可用格子。 </summary>
        public void ClampToWalkable(ref FVector2 pos, out FVector2Int mapPos, bool value = false, int layer = 0)
        {
            var rect = WorldRect;
            if (pos.X < rect.Min.X)
                pos.X = rect.Min.X;
            else if (pos.X >= rect.Max.X)
                pos.X = rect.Max.X - FNum.Thousandth;

            if (pos.Y < rect.Min.Y)
                pos.Y = rect.Min.Y;
            else if (pos.Y >= rect.Max.Y)
                pos.Y = rect.Max.Y - FNum.Thousandth;

            mapPos = WorldToMapPos(pos);
            if (!CheckTile(mapPos, value, layer))
            {
                var nearPos = FindNearPos(mapPos, value: value, layer: layer);
                if (nearPos != FVector2Int.None)
                {
                    mapPos = nearPos;
                    pos = MapToWorldPos(mapPos);
                }
            }
        }

        /// <summary> 返回地图的简要文本表示。 </summary>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary> 返回地图的简要或完整文本表示。 </summary>
        public string ToString(bool isVerbose)
        {
            if (Terrain == null)
                return string.Empty;
            if (!isVerbose)
            {
                var summary = new StringBuilder(16 + Terrain.Length * 4).Append("layers:").Append(Terrain.Length).Append('|');
                for (var i = 0; i < Terrain.Length; i++)
                {
                    if (i > 0)
                        summary.Append(',');
                    summary.Append(Terrain[i].Length);
                }
                return summary.ToString();
            }

            var strbuf = new StringBuilder(TerrainSize.X * TerrainSize.Y * Terrain.Length * 2);
            strbuf.AppendLine($"Layers {Terrain.Length}");
            for (var i = 0; i < Terrain.Length; i++)
            {
                for (var y = 0; y < TerrainSize.Y; y++)
                {
                    for (var x = 0; x < TerrainSize.X; x++)
                        strbuf.Append(this[i, x, y] ? '1' : '0').Append('\t');
                    strbuf.AppendLine();
                }
            }

            return strbuf.ToString();
        }

        /// <summary> 将各层位图拆分为低位在前的 32 位整数数组。 </summary>
        public int[][] ToIntArray(int[][] dest = null)
        {
            if (dest == null || dest.Length != Terrain.Length)
                dest = new int[Terrain.Length][];
            for (var i = 0; i < Terrain.Length; i++)
            {
                var terrain = Terrain[i];
                var intCount = terrain.Length * 2;
                var ints = dest[i];
                if (ints == null || ints.Length != intCount)
                    ints = dest[i] = new int[intCount];
                for (var j = 0; j < terrain.Length; j++)
                {
                    var index = j << 1;
                    ints[index] = unchecked((int)terrain[j]);
                    ints[index + 1] = unchecked((int)(terrain[j] >> 32));
                }
            }

            return dest;
        }

        /// <summary> 检查矩形位置和自定义条件是否有效。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckPosition(in FVector2Int pos, in FVector2Int size, bool value, Func<QuadMap, FVector2Int, bool> checker, int layer) => CheckTile(pos, size, value, layer) && checker?.Invoke(this, pos) != false;

        /// <summary> 检查单格位置、黑名单和自定义条件是否有效。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckPosition(in FVector2Int pos, bool value, Func<QuadMap, FVector2Int, bool> checker, HashSet<FVector2Int> blackPositions, int layer) => CheckTile(pos, value, layer) && blackPositions?.Contains(pos) != true && checker?.Invoke(this, pos) != false;

        /// <summary> 将源位图中的连续位复制到目标位图。 </summary>
        private static void CopyBits(ulong[] source, int sourceIndex, ulong[] destination, int destinationIndex, int count)
        {
            if (((sourceIndex | destinationIndex) & 63) == 0)
            {
                var wordCount = count >> 6;
                var sourceWordIndex = sourceIndex >> 6;
                if (wordCount > 0 && sourceWordIndex + wordCount <= source.Length)
                {
                    Array.Copy(source, sourceWordIndex, destination, destinationIndex >> 6, wordCount);
                    var copiedBits = wordCount << 6;
                    sourceIndex += copiedBits;
                    destinationIndex += copiedBits;
                    count -= copiedBits;
                }
            }

            while (count > 0)
            {
                var copyCount = Math.Min(count, BitArrayOperator.BitSize);
                WriteBits(destination, destinationIndex, ReadBits(source, sourceIndex, copyCount));
                sourceIndex += copyCount;
                destinationIndex += copyCount;
                count -= copyCount;
            }
        }

        /// <summary> 读取最多 64 个连续位，并将首位对齐到最低位。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadBits(ulong[] bits, int index, int count)
        {
            var wordIndex = index >> 6;
            if ((uint)wordIndex >= (uint)bits.Length)
                return 0;
            var offset = index & 63;
            var result = bits[wordIndex] >> offset;
            if (offset != 0 && wordIndex + 1 < bits.Length)
                result |= bits[wordIndex + 1] << (BitArrayOperator.BitSize - offset);
            return result & GetBitMask(count);
        }

        /// <summary> 将最低位对齐的连续位合并到目标位图。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBits(ulong[] bits, int index, ulong value)
        {
            var wordIndex = index >> 6;
            var offset = index & 63;
            bits[wordIndex] |= value << offset;
            if (offset != 0 && wordIndex + 1 < bits.Length)
                bits[wordIndex + 1] |= value >> (BitArrayOperator.BitSize - offset);
        }

        /// <summary> 获取低位连续置一的位掩码。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetBitMask(int count) => count == BitArrayOperator.BitSize ? ulong.MaxValue : (1UL << count) - 1;

        /// <summary> 使用整数八分区计算最接近目标方向的邻格索引。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNearestDirectionIndex(in FVector2Int from, in FVector2Int to)
        {
            const long scale = 1_000_000_000;
            const long tan22_5Scaled = 414_213_562;
            var dx = (long)to.X - from.X;
            var dy = (long)to.Y - from.Y;
            if ((dx | dy) == 0)
                return 0;
            var absX = Math.Abs(dx);
            var absY = Math.Abs(dy);
            if (absY * scale < absX * tan22_5Scaled)
                return dx >= 0 ? 0 : 4;
            if (absX * scale < absY * tan22_5Scaled)
                return dy >= 0 ? 2 : 6;
            if (dx >= 0)
                return dy >= 0 ? 1 : 7;
            return dy >= 0 ? 3 : 5;
        }

        #region BytesSerializer

        /// <summary> 将地图数据写入字节流。 </summary>
        public virtual void Z_BytesWrite(ref BytesWriter writer)
        {
            writer.Push(Offset);
            writer.Push(TileSize);
            writer.Push(TerrainSize);
            writer.Push(Terrain);
        }

        /// <summary> 从字节流读取地图数据。 </summary>
        public virtual void Z_BytesRead(ref BytesReader reader)
        {
            reader.Read(ref Offset);
            reader.Read(ref TileSize);
            reader.Read(ref TerrainSize);
            Terrain = reader.ReadArray2<ulong>();
        }

        #endregion
    }
}
