//==================={By Qcbf|qcbf@qq.com|9/14/2023 6:01:36 PM}===================

using System;
using System.Collections.Generic;

namespace FLib
{
    /// <summary>
    /// aabb, [Min, End] 半开区间
    /// </summary>
    [Serializable]
    public struct FRectInt : IEquatable<FRectInt>, IJson5Deserializable
    {
        public FVector2Int Min;
        public FVector2Int End;

        public readonly int Width => End.X - Min.X;
        public readonly int Height => End.Y - Min.Y;
        public readonly FVector2Int Size => End - Min;
        public readonly FVector2Int Center => Min + Size / 2;

        public FRectInt(in FVector2Int min, in FVector2Int end)
        {
            Min = min;
            End = end;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Expand(in FVector2Int xy)
        {
            Min -= xy;
            End += xy;
        }

        /// <summary>
        /// 
        /// </summary>
        public void TryUpdateMinMax(FVector2Int point)
        {
            Min.X = Math.Min(Min.X, point.X);
            Min.Y = Math.Min(Min.Y, point.Y);
            End.X = Math.Max(End.X, point.X + 1);
            End.Y = Math.Max(End.Y, point.Y + 1);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Add(in FVector2Int xy)
        {
            Min += xy;
            End += xy;
        }

        public static FRectInt CreateByCenter(in FVector2Int center, in FVector2Int extends) => new(center - extends, center + extends);
        public readonly override string ToString() => $"{Min},{End}";
        public readonly bool Contains(in FVector2Int point) => point >= Min && point < End;
        public readonly bool Contains(in FVector2Int point, in FVector2Int expand) => point >= Min - expand && point < End + expand;

        public readonly bool TrimPoint(ref FVector2Int point)
        {
            var result = false;
            if (point.X < Min.X)
            {
                point.X = Min.X;
                result = true;
            }
            else if (point.X >= End.X)
            {
                point.X = End.X - 1;
                result = true;
            }

            if (point.Y < Min.Y)
            {
                point.Y = Min.Y;
                result = true;
            }
            else if (point.Y >= End.Y)
            {
                point.Y = End.Y - 1;
                result = true;
            }

            return result;
        }

        readonly bool IEquatable<FRectInt>.Equals(FRectInt other) => Min == other.Min && End == other.End;


        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
        {
            Span<int> vals = stackalloc int[4];
            FVector2Int.JsonParseHelper(ref nodes, ref vals);
            Min.X = vals[0];
            Min.Y = vals[1];
            End.X = vals[2];
            End.Y = vals[3];
            return true;
        }
    }
}