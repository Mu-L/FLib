// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;

namespace FLib
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct IntRange : IEquatable<IntRange>, IJson5Deserializable
    {
        public int Start;
        public int End;

        public readonly int Length => End - Start;

        public IntRange(int value) => End = (Start = value) + 1;

        public IntRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int AddStart(int v) => Start += v;
        public int AddEnd(int v) => End += v;
        public bool Contains(int v) => v >= Start && v < End;

        public override string ToString() => $"{Start}-{End}";
        public static implicit operator Range(in IntRange range) => new(range.Start, range.End);
        public static implicit operator IntRange(in Range range) => new(range.Start.Value, range.End.Value);
        public static implicit operator IntRange(in int value) => new(value);
        public bool Equals(IntRange other) => Start == other.Start && End == other.End;
        public override bool Equals(object obj) => obj is IntRange range && Equals(range);
        public override int GetHashCode() => HashCode.Combine(Start, End);
        public static bool operator ==(in IntRange left, in IntRange right) => left.Start == right.Start && left.End == right.End;
        public static bool operator !=(in IntRange left, in IntRange right) => !(left == right);

        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
        {
            if (!Json5SyntaxNodesReader.TryCreate(ref nodes, out var node, out var reader, EJson5Token.ArrayOpen | EJson5Token.Value)) return false;
            if (node.Token == EJson5Token.Value)
            {
                End = (Start = node.ContentSpan.ToInt()) + 1;
            }
            else
            {
                Start = reader.Read(ref nodes).ContentSpan.ToInt();
                End = reader.TryRead(ref nodes, out node) ? node.ContentSpan.ToInt() : Start + 1;
            }

            reader.Close(ref nodes);
            return true;
        }
    }
}