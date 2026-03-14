// =================================================={By Qcbf|qcbf@qq.com|2024-6-11}==================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FLib.Unity
{
    public static class UnityTypeSerializationSupporter
    {
        public static void Register()
        {
            Json5.CustomDeserializers ??= new Dictionary<Type, IJson5Deserializable>(8);
            Json5.CustomSerializers ??= new Dictionary<Type, IJson5Serializable>(8);
            Json5.CustomDeserializers.Add(typeof(Vector2), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                Span<FNum> vals = stackalloc FNum[2];
                FVector2.JsonParseHelper(ref nodes, ref vals);
                return new Json5CustomDeserializeResult(new Vector2(vals[0], vals[1]));
            }));
            Json5.CustomSerializers.Add(typeof(Vector2), new Json5CustomSerializeWrap((obj, _, _, _) => obj is Vector2 vec ? $"[{vec.x:0.###},{vec.y:0.###}]" : "[]"));
            Json5.CustomDeserializers.Add(typeof(Vector3), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                Span<FNum> vals = stackalloc FNum[3];
                FVector2.JsonParseHelper(ref nodes, ref vals);
                return new Json5CustomDeserializeResult(new Vector3(vals[0], vals[1], vals[2]));
            }));
            Json5.CustomSerializers.Add(typeof(Vector3), new Json5CustomSerializeWrap((obj, _, _, _) => obj is Vector3 vec ? $"[{vec.x:0.###},{vec.y:0.###},{vec.z:0.###}]" : "[]"));
            Json5.CustomDeserializers.Add(typeof(Vector2Int), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                Span<int> vals = stackalloc int[2];
                FVector2Int.JsonParseHelper(ref nodes, ref vals);
                return new Json5CustomDeserializeResult(new Vector2Int(vals[0], vals[1]));
            }));
            Json5.CustomSerializers.Add(typeof(Vector2Int), new Json5CustomSerializeWrap((obj, _, _, _) => obj is Vector2Int vec ? $"[{vec.x},{vec.y}]" : "[]"));
            Json5.CustomDeserializers.Add(typeof(Vector3Int), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                Span<int> vals = stackalloc int[3];
                FVector2Int.JsonParseHelper(ref nodes, ref vals);
                return new Json5CustomDeserializeResult(new Vector3Int(vals[0], vals[1]));
            }));
            Json5.CustomSerializers.Add(typeof(Vector3Int), new Json5CustomSerializeWrap((obj, _, _, _) => obj is Vector3Int vec ? $"[{vec.x},{vec.y},{vec.z}]" : "[]"));
            Json5.CustomDeserializers.Add(typeof(Rect), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                Span<FNum> vals = stackalloc FNum[4];
                FVector2.JsonParseHelper(ref nodes, ref vals);
                return new Json5CustomDeserializeResult(new Rect(vals[0], vals[1], vals[2], vals[3]));
            }));
            Json5.CustomSerializers.Add(typeof(Rect), new Json5CustomSerializeWrap((obj, _, _, _) => obj is Rect v ? $"[{v.x:0.###},{v.y:0.###},{v.width:0.###},{v.height:0.###}]" : "[]"));
            Json5.CustomDeserializers.Add(typeof(RectInt), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                Span<int> vals = stackalloc int[4];
                FVector2Int.JsonParseHelper(ref nodes, ref vals);
                return new Json5CustomDeserializeResult(new RectInt(vals[0], vals[1], vals[2], vals[3]));
            }));
            Json5.CustomSerializers.Add(typeof(RectInt), new Json5CustomSerializeWrap((obj, _, _, _) => obj is RectInt v ? $"[{v.x},{v.y},{v.width},{v.height}]" : "[]"));
            Json5.CustomDeserializers.Add(typeof(Color), new Json5CustomDeserializeWrap((ref Json5SyntaxNodes nodes, object customData, in Json5DeserializeOptionData options) =>
            {
                if (!nodes.TryMoveNextValueOrCloseToken(out var node))
                    return false;
                ColorUtility.TryParseHtmlString(node.ContentSpan.ToString(), out var color);
                return new Json5CustomDeserializeResult(color);
            }));
            Json5.CustomSerializers.Add(typeof(Color), new Json5CustomSerializeWrap((obj, _, _, _) => obj is Color v ? ColorUtility.ToHtmlStringRGBA(v) : string.Empty));
        }
    }
}
