// ==================={By Qcbf|qcbf@qq.com|10/14/2022 10:56:47 AM}===================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using FLib;

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
        public int LayerCount => Terrain.Length;

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
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => this[layer, PosToIdx(new FVector2Int(x, y))];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => this[layer, PosToIdx(new FVector2Int(x, y))] = value;
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
            to.Terrain = Terrain.Select(v => (ulong[])v.Clone()).ToArray();
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
            Array.Resize(ref Terrain, count);
            var tileCount = BitArrayOperator.GetBitsLength(TerrainSize.X * TerrainSize.Y);
            for (var i = 0; i < LayerCount; i++)
                Array.Resize(ref Terrain[i], tileCount);
            return this;
        }

        /// <summary> 设置地图尺寸和可选层数，并保留交集区域的位数据。 </summary>
        public virtual QuadMap SetSize(in FVector2Int size, int layerCount = -1)
        {
            var oldSize = TerrainSize;
            var w = Math.Min(oldSize.X, size.X);
            var h = Math.Min(oldSize.Y, size.Y);
            TerrainSize = size;
            if (layerCount > 0)
                Array.Resize(ref Terrain, layerCount);
            else if (Terrain == null)
                Terrain = new ulong[1][];
            var tileCount = BitArrayOperator.GetBitsLength(size.X * size.Y);
            for (var i = 0; i < LayerCount; i++)
            {
                var oldTerrain = Terrain[i];
                var newTerrain = new ulong[tileCount];
                if (oldTerrain != null)
                {
                    for (var y = 0; y < h; y++)
                    {
                        for (var x = 0; x < w; x++)
                        {
                            if (BitArrayOperator.GetBit(oldTerrain, y * oldSize.X + x))
                                BitArrayOperator.SetBit(newTerrain, y * size.X + x, true);
                        }
                    }
                }

                Terrain[i] = newTerrain;
            }

            return this;
        }

        /// <summary> 检查格子坐标是否位于地图边界内。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckTileValid(in FVector2Int pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < TerrainSize.X && pos.Y < TerrainSize.Y;

        /// <summary> 检查指定层的格子是否位于地图内且具有目标值。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckTile(in FVector2Int pos, bool value, int layer = 0) => CheckTileValid(pos) && BitArrayOperator.GetBit(Terrain[layer], PosToIdx(pos)) == value;

        /// <summary> 检查指定层的矩形区域是否位于地图内且全部具有目标值。 </summary>
        public bool CheckTile(in FVector2Int pos, FVector2Int size, bool value = false, int layer = 0)
        {
            for (var y = 0; y < size.Y; y++)
            {
                for (var x = 0; x < size.X; x++)
                {
                    if (!CheckTile(new FVector2Int(pos.X + x, pos.Y + y), value, layer))
                        return false;
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
            if (CheckTile(pos, size, value, layer) && checker?.Invoke(this, pos) != false)
                return pos;
            if (findMaxDist <= 0)
                findMaxDist = Math.Max(TerrainSize.X, TerrainSize.Y);
            else
                ++findMaxDist;
            for (var i = 1; i < findMaxDist; i++)
            {
                var from = pos - i;
                var to = pos + i;
                for (var x = -i; x <= i; x++)
                {
                    var mapPos = new FVector2Int(pos.X + x, from.Y);
                    if (CheckTile(mapPos, size, value, layer) && checker?.Invoke(this, mapPos) != false)
                        return mapPos;
                    mapPos.Y = to.Y;
                    if (CheckTile(mapPos, size, value, layer) && checker?.Invoke(this, mapPos) != false)
                        return mapPos;
                }

                for (var y = -i + 1; y < i; y++)
                {
                    var mapPos = new FVector2Int(from.X, pos.Y + y);
                    if (CheckTile(mapPos, size, value, layer) && checker?.Invoke(this, mapPos) != false)
                        return mapPos;
                    mapPos.X = to.X;
                    if (CheckTile(mapPos, size, value, layer) && checker?.Invoke(this, mapPos) != false)
                        return mapPos;
                }
            }

            return FVector2Int.None;
        }

        /// <summary> 沿目标方向查找指定层的下一步可用格子。 </summary>
        public FVector2Int FindNearNextStepPos(in FVector2Int from, in FVector2Int to, bool value = false, Func<QuadMap, FVector2Int, bool> checker = null, HashSet<FVector2Int> blackPositions = null, int layer = 0)
        {
            var segment = (FNum)45;
            var half = segment * FNum.OneHalf;
            var index = (int)((FVector2.Angle360(FVector2.Right, to - from) + half) / segment) % 8;
            var next = from + NearestEightPositions[index];
            if (CheckTile(next, value, layer) && blackPositions?.Contains(next) != true && checker?.Invoke(this, next) != false)
                return next;
            for (var i = 1; i < 4; i++)
            {
                next = from + NearestEightPositions[(index + i) % 8];
                if (CheckTile(next, value, layer) && blackPositions?.Contains(next) != true && checker?.Invoke(this, next) != false)
                    return next;
                next = from + NearestEightPositions[(index - i + 8) % 8];
                if (CheckTile(next, value, layer) && blackPositions?.Contains(next) != true && checker?.Invoke(this, next) != false)
                    return next;
            }

            next = from + NearestEightPositions[(index + 4) % 8];
            if (CheckTile(next, value, layer) && blackPositions?.Contains(next) != true && checker?.Invoke(this, next) != false)
                return next;
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
                return $"layers:{Terrain.Length}|{string.Join(',', Terrain.Select(v => v.Length))}";

            var strbuf = new StringBuilder(TerrainSize.X * TerrainSize.Y * Terrain.Length);
            strbuf.AppendLine($"Layers {Terrain.Length}");
            for (var i = 0; i < Terrain.Length; i++)
            {
                for (var y = 0; y < TerrainSize.Y; y++)
                {
                    for (var x = 0; x < TerrainSize.X; x++)
                        strbuf.Append(Convert.ToInt32(this[i, x, y])).Append('\t');
                    strbuf.AppendLine();
                }
            }

            return strbuf.ToString();
        }

        /// <summary> 将各层位图拆分为低位在前的 32 位整数数组。 </summary>
        public int[][] ToIntArray(int[][] dest = null)
        {
            dest ??= new int[Terrain.Length][];
            for (var i = 0; i < Terrain.Length; i++)
            {
                var terrain = Terrain[i];
                var ints = dest[i] = new int[terrain.Length * 2];
                for (var j = 0; j < terrain.Length; j++)
                {
                    ints[j * 2] = (int)terrain[j];
                    ints[j * 2 + 1] = (int)(terrain[j] >> 32);
                }
            }

            return dest;
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
