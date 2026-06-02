// ==================== qcbf@qq.com | 2025-07-01 ====================

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FLib
{
    /// <summary>
    /// nonsupport: 未转义换行符
    /// </summary>
    public static class Json5
    {
        public static Dictionary<Type, IJson5Deserializable>? CustomDeserializers;
        public static Dictionary<Type, IJson5Serializable>? CustomSerializers;
        public static HashSet<Type>? NonSerialized;

        // ReSharper disable Unity.PerformanceAnalysis
        public static string SerializeToLog(object? val, Json5SerializeOptionData opData = default)
        {
            opData.Options |= EJson5SerializeOption.LogText;
            return Json5Serializer.PushValue(val, new StringBuilder(), 0, opData).ToString();
        }

        public static string Serialize(object? val, Json5SerializeOptionData opData = default) => Json5Serializer.PushValue(val, new StringBuilder(), 0, opData).ToString();
        public static T Deserialize<T>(string source, Json5DeserializeOptionData opData = default) => (T)Deserialize(source, typeof(T), opData);
        public static object Deserialize(string source, Json5DeserializeOptionData opData = default) => Deserialize(source, typeof(object), opData);

        public static object Deserialize(string source, Type toType, Json5DeserializeOptionData opData = default)
        {
            var nodes = DeserializeToSyntaxNodes(source, opData);
            if (nodes.Count == 0)
                return toType.DefaultValue();
            var obj = Json5Deserializer.ToValue(ref nodes, toType, opData);
            nodes.Nodes.Dispose();
            return obj;
        }

        public static Json5SyntaxNodes DeserializeToSyntaxNodes(string source, Json5DeserializeOptionData options = default)
        {
            var nodes = new Json5SyntaxNodes() { Nodes = new PooledList<Json5SyntaxNode>(128) };
            var node = new Json5SyntaxNode() { FullSource = source };
            while (node.RemainingLength > 0)
            {
                node.Token = default;
                node.SourceRange = node.ContentRange = new IntRange(node.SourceRange.End);
                node.Parse(options);
                if (node.Token != EJson5Token.None && (node.Token != EJson5Token.Comment || options.IsKeepCommentSyntaxNode) &&
                    (node.Token != EJson5Token.Skip || options.IsKeepSkipSyntaxNode))
                    nodes.Nodes.Add(node);
            }

            return nodes;
        }
    }

    #region Serialize

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum EJson5SerializeOption
    {
        None,

        /// <summary>
        /// 兼容模式，兼容json1
        /// </summary>
        Compatible = 0x1,

        /// <summary>
        /// 只序列化标记了Serializable的字段
        /// </summary>
        OnlySerializableFields = 0x2,

        /// <summary>
        /// 格式化输出：短内容单行紧凑，长或嵌套内容多行缩进，兼顾可读性与紧凑性
        /// </summary>
        Pretty = 0x4,

        /// <summary>
        /// 包含空字符串的字段，最终得到 Field:""
        /// </summary>
        IncludeEmptyStringField = 0x8,

        /// <summary>
        /// 日志方式序列化，如果类型有override ToString那么就直接调用ToString而不是序列化每个字段
        /// </summary>
        LogText = 0x10,

        /// <summary>
        /// 保留字符串原始内容，而不添加转义字符和双引号
        /// </summary>
        RetainString = 0x20,

        /// <summary>
        /// 不要写入字典的空key， {"a":11, "":22}得到a:11, 22而不是 a:11, "":22， 方便做一些特殊的json值
        /// </summary>
        DictDoNotWriteEmptyKeyWithColonChar = 0x40,
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Json5SerializeOptionData
    {
        public EJson5SerializeOption Options;
        public object? CustomData;
        public readonly bool Op(EJson5SerializeOption op) => (Options & op) == op;
        public static implicit operator Json5SerializeOptionData(EJson5SerializeOption options) => new() { Options = options };
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IJson5Serializable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonText"></param>
        /// <param name="serializeObject"></param>
        /// <param name="customData">目前有一下情况:解析对象时的字段字符串名称, 外部调用PushObject传入的值</param>
        /// <param name="indent"></param>
        /// <param name="opData"></param>
        /// <returns>是否已经处理, 如果返回true则跳过正常解析</returns>
        bool JsonSerialize(StringBuilder jsonText, object serializeObject, object? customData, int indent, Json5SerializeOptionData opData);
    }

    /// <summary>
    /// 
    /// </summary>
    public class Json5CustomSerializeWrap : IJson5Serializable
    {
        public Func<StringBuilder, object, object?, int, Json5SerializeOptionData, bool> Handler;
        public Json5CustomSerializeWrap(Func<StringBuilder, object, object?, int, Json5SerializeOptionData, bool> handler) => Handler = handler;
        public bool JsonSerialize(StringBuilder jsonText, object serializeObject, object? customData, int indent, Json5SerializeOptionData opData) => Handler(jsonText, serializeObject, customData, indent, opData);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Json5Serializer
    {
        private const int PrettyInlineMaxLen = 120;

        /// <summary>
        /// 
        /// </summary>
        public static StringBuilder PushValue(object? obj, StringBuilder strbuf, int indent, Json5SerializeOptionData opData)
        {
            // strbuf.Append('\t', indent);
            switch (obj)
            {
                case null:
                    if (opData.Op(EJson5SerializeOption.Compatible))
                        strbuf.Append('"', 2);
                    break;
                case string:
                case Enum:
                    if (opData.Op(EJson5SerializeOption.LogText) || opData.Op(EJson5SerializeOption.RetainString))
                    {
                        strbuf.Append(obj);
                    }
                    else
                    {
                        var str = obj.ToString();
                        strbuf.Capacity += str!.Length + 2;
                        strbuf.Append('"');
                        foreach (var c in str)
                        {
                            if (c == '"')
                                strbuf.Append('\\');
                            strbuf.Append(c);
                        }

                        strbuf.Append('"');
                    }

                    break;
                case IDictionary val:
                    PushDict(val, strbuf, indent, opData);
                    break;
                case IEnumerable val:
                    PushArray(val, strbuf, indent, opData);
                    break;
                case float val:
                    strbuf.Append(val.ToString("0.#####"));
                    break;
                case FNum val:
                    strbuf.Append(val.ToString("0.#####"));
                    break;
                case double val:
                    strbuf.Append(val.ToString("0.########"));
                    break;
                case DateTime val:
                    if (opData.Op(EJson5SerializeOption.Compatible))
                        strbuf.Append('"');
                    strbuf.Append(val.ToString("yyyy-MM-dd_HH-mm-ss"));
                    if (opData.Op(EJson5SerializeOption.Compatible))
                        strbuf.Append('"');
                    break;
                default:
                    switch (obj)
                    {
                        case int or uint or long or sbyte or byte or short or ushort or ulong:
                            strbuf.Append(obj);
                            break;
                        case bool:
                            strbuf.Append(obj.ToString()!.ToLowerInvariant());
                            break;
                        default:
                            PushObject(obj, strbuf, indent, opData);
                            break;
                    }

                    break;
            }

            return strbuf;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PushArray(IEnumerable array, StringBuilder strbuf, int indent, Json5SerializeOptionData opData)
        {
            IEnumerator iterator;
            try
            {
                // ReSharper disable once GenericEnumeratorNotDisposed
                iterator = array.GetEnumerator(); // default ArraySegment会异常
            }
            catch
            {
                return;
            }

            if (opData.Op(EJson5SerializeOption.Pretty) && !opData.Op(EJson5SerializeOption.LogText))
            {
                var elements = new List<string>();
                var buf = new StringBuilder();
                while (iterator.MoveNext())
                {
                    buf.Clear();
                    PushValue(iterator.Current, buf, indent + 1, opData);
                    elements.Add(buf.ToString());
                }

                AppendPrettyBlock(elements, strbuf, indent, '[', ']');
                return;
            }

            strbuf.Append('[');
            var isMoveNext = false;
            try
            {
                isMoveNext = iterator.MoveNext();
            }
            catch
            {
                // ignored
            }

            if (isMoveNext)
            {
                PushValue(iterator.Current, strbuf, indent, opData);
                if (opData.Op(EJson5SerializeOption.LogText))
                {
                    var i = 0;
                    while (iterator.MoveNext())
                    {
                        if (++i > 512)
                        {
                            strbuf.Append(',').Append('"').Append("more...");
                            if (array is ICollection coll)
                                strbuf.Append("total: ").Append(coll.Count);
                            strbuf.Append('"');
                            break;
                        }

                        strbuf.Append(',');
                        PushValue(iterator.Current, strbuf, indent, opData);
                    }
                }
                else
                {
                    while (iterator.MoveNext())
                    {
                        strbuf.Append(',');
                        PushValue(iterator.Current, strbuf, indent, opData);
                    }
                }
            }

            strbuf.Append(']');
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PushDict(IDictionary dict, StringBuilder strbuf, int indent, Json5SerializeOptionData opData)
        {
            // ReSharper disable AssignNullToNotNullAttribute
            // ReSharper disable GenericEnumeratorNotDisposed
            if (opData.Op(EJson5SerializeOption.Pretty))
            {
                var entries = new List<string>();
                var buf = new StringBuilder();
                var it = dict.GetEnumerator();
                while (it.MoveNext())
                {
                    buf.Clear();
                    PushDictKey(it.Key, buf, indent + 1, opData);
                    PushValue(it.Value, buf, indent + 1, opData);
                    entries.Add(buf.ToString());
                }

                AppendPrettyBlock(entries, strbuf, indent, '{', '}');
                return;
            }

            strbuf.Append('{');
            var iterator = dict.GetEnumerator();
            if (iterator.MoveNext())
            {
                PushDictKey(iterator.Key, strbuf, indent, opData);
                PushValue(iterator.Value, strbuf, indent, opData);
            }

            while (iterator.MoveNext())
            {
                strbuf.Append(',');
                PushDictKey(iterator.Key, strbuf, indent, opData);
                PushValue(iterator.Value, strbuf, indent, opData);
            }

            strbuf.Append('}');
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PushDictKey(object key, StringBuilder strbuf, int indent, Json5SerializeOptionData opData)
        {
            if ((opData.Options & EJson5SerializeOption.DictDoNotWriteEmptyKeyWithColonChar) == 0 || key is not string strKey || !string.IsNullOrWhiteSpace(strKey))
            {
                if (opData.Op(EJson5SerializeOption.Compatible))
                    strbuf.Append('"');
                strbuf.Append(key);
                if (opData.Op(EJson5SerializeOption.Compatible))
                    strbuf.Append('"');
                strbuf.Append(':');
                if (opData.Op(EJson5SerializeOption.Pretty))
                    strbuf.Append(' ');
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PushObject(object obj, StringBuilder strbuf, int indent, Json5SerializeOptionData opData, object? customData = null)
        {
            var t = obj.GetType();
            var declaringType = (opData.Options & EJson5SerializeOption.LogText) == 0
                ? null
                : t.GetMethod(nameof(ToString), BindingFlags.Public | BindingFlags.Instance, null, Array.Empty<Type>(), null)?.DeclaringType;
            if (declaringType != null && declaringType != typeof(object) && declaringType != typeof(ValueType))
            {
                strbuf.Append(obj);
                return;
            }

            if (Json5.CustomSerializers == null || !Json5.CustomSerializers.TryGetValue(t, out var serializer))
                serializer = obj as IJson5Serializable;
            if (serializer?.JsonSerialize(strbuf, obj, customData, indent, opData) == true)
                return;

            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var len = fields.Length;

            if (opData.Op(EJson5SerializeOption.Pretty))
            {
                var fieldEntries = new List<string>();
                var buf = new StringBuilder();
                for (var i = 0; i < len; i++)
                {
                    buf.Clear();
                    if (PushField(obj, fields[i], buf, indent + 1, opData) && buf.Length > 0)
                        fieldEntries.Add(buf.ToString());
                }

                AppendPrettyBlock(fieldEntries, strbuf, indent, '{', '}');
                return;
            }

            strbuf.Append('{');
            for (var i = 0; i < len; i++)
            {
                var success = PushField(obj, fields[i], strbuf, indent, opData);
                if (success && i < len - 1)
                    strbuf.Append(',');
            }

            strbuf.Append('}');
            return;

            static bool PushField(object obj, FieldInfo field, StringBuilder strbuf, int indent, Json5SerializeOptionData opData)
            {
                if (field.IsInitOnly || field.IsLiteral || ((opData.Options & EJson5SerializeOption.OnlySerializableFields) != 0 && !field.IsDefined(typeof(SerializableAttribute))) ||
                    (Json5.NonSerialized != null && (Json5.NonSerialized.Contains(field.FieldType) || Json5.NonSerialized.Contains(field.FieldType.BaseType))) ||
                    field.IsDefined(typeof(NonSerializedAttribute)) || field.FieldType.IsSubclassOf(typeof(Delegate)))
                    return false;

                var fieldName = field.Name;
                if ((obj as IJson5Serializable)?.JsonSerialize(strbuf, obj, fieldName, indent, opData) == true)
                    return true;
                var val = field.GetValue(obj);
                if (val != null && (val is not string str || str.Length > 0 || (opData.Options & EJson5SerializeOption.IncludeEmptyStringField) != 0))
                {
                    PushDictKey(fieldName, strbuf, indent, opData);
                    PushValue(val, strbuf, indent, opData);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AppendPrettyBlock(List<string> entries, StringBuilder strbuf, int indent, char open, char close)
        {
            strbuf.Append(open);
            if (entries.Count == 0)
            {
                strbuf.Append(close);
                return;
            }

            var containsNewline = false;
            var totalLen = 0;
            foreach (var e in entries)
            {
                totalLen += e.Length;
                if (e.IndexOf('\n') >= 0) containsNewline = true;
            }

            if (!containsNewline && totalLen + (entries.Count - 1) * 2 <= PrettyInlineMaxLen)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    if (i > 0) strbuf.Append(", ");
                    strbuf.Append(entries[i]);
                }
            }
            else
            {
                var newIndent = indent + 1;
                strbuf.Append('\n');
                for (var i = 0; i < entries.Count; i++)
                {
                    strbuf.Append('\t', newIndent);
                    strbuf.Append(entries[i]);
                    if (i < entries.Count - 1) strbuf.Append(',');
                    strbuf.Append('\n');
                }

                strbuf.Append('\t', indent);
            }

            strbuf.Append(close);
        }
    }

    #endregion

    #region Deserialize

    /// <summary>
    /// 
    /// </summary>
    public class Json5CustomDeserializeWrap : IJson5Deserializable
    {
        public Delegate Handler;
        public Json5CustomDeserializeWrap(Delegate handler) => Handler = handler;

        public delegate Json5CustomDeserializeResult Delegate(ref Json5SyntaxNodes nodes, object? customData, in Json5DeserializeOptionData options);

        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object? otherData, in Json5DeserializeOptionData options) => Handler(ref nodes, otherData, options);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IJson5Deserializable
    {
        Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object? otherData, in Json5DeserializeOptionData options);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IJson5FieldDeserializable
    {
        Json5CustomDeserializeResult JsonDeserialize(string fieldName, ref Json5SyntaxNodes nodes, object? otherData, in Json5DeserializeOptionData options);
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Json5DeserializeOptionData
    {
        internal bool IsFallback;
        public bool IsKeepCommentSyntaxNode;
        public bool IsKeepSkipSyntaxNode;
        public bool IsIgnoreMissingField;
        public object UserData;
        public Func<string, string?> FieldNameFallback;
    }

    /// <summary>
    /// 
    /// </summary>
    public ref struct Json5CustomDeserializeResult
    {
        /// <summary>
        /// 0: not hooked, 1: hooked, 2: hooked with force use Result
        /// </summary>
        public byte HookedType;

        public object? Result;
        public bool IsHooked => HookedType > 0;

        public Json5CustomDeserializeResult(object? result, byte hookedType = 1)
        {
            HookedType = hookedType;
            Result = result;
        }

        public static implicit operator Json5CustomDeserializeResult(bool v) => new() { HookedType = (byte)(v ? 1 : 0) };
        public static implicit operator Json5CustomDeserializeResult(byte v) => new() { HookedType = v };
        public static implicit operator bool(in Json5CustomDeserializeResult v) => v.HookedType > 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Json5SyntaxNodes : IEnumerable<Json5SyntaxNode>, IDisposable
    {
        public PooledList<Json5SyntaxNode> Nodes;
        public int Position;
        public readonly ArraySegment<Json5SyntaxNode> Segment => Nodes.Array[Position..];
        public readonly int Count => Nodes.Count - Position;
        public ref Json5SyntaxNode Current => ref Nodes[Position];
        public ref Json5SyntaxNode this[int index] => ref Nodes[Position + index];
        public Json5SyntaxNode MoveNext() => Nodes[Position++];
        public void Dispose() => Nodes.Dispose();

        /// <summary>
        /// 
        /// </summary>
        public Json5SyntaxNode GetLastToken(EJson5Token token)
        {
            for (var i = Position - 1; i >= 0; i--)
            {
                if ((Nodes[i].Token & token) != 0)
                    return Nodes[i];
            }

            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TryMoveNextValueOrCloseTokenThenClose(out Json5SyntaxNode node)
        {
            if (TryMoveNextValueOrCloseToken(out node))
            {
                MoveNext(EJson5Token.Close);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TryMoveNextValueOrCloseToken(out Json5SyntaxNode node)
        {
            node = MoveNext(EJson5Token.Value | EJson5Token.Close);
            return node.Token == EJson5Token.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        public Json5SyntaxNode MoveNext(EJson5Token token)
        {
            while (Position < Nodes.Count)
            {
                if ((Nodes[Position++].Token & token) != 0)
                    return Nodes[Position - 1];
            }

            return default;
        }

        public T To<T>(Json5DeserializeOptionData options = default) => (T)Json5Deserializer.ToValue(ref this, typeof(T), options);
        public object To(Type toType, Json5DeserializeOptionData options = default) => Json5Deserializer.ToValue(ref this, toType, options);
        public ArraySegment<Json5SyntaxNode>.Enumerator GetEnumerator() => Segment.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<Json5SyntaxNode> IEnumerable<Json5SyntaxNode>.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// 
    /// </summary>
    public ref struct Json5SyntaxNodesReader
    {
        public byte BracketOpenCount;

        /// <summary>
        /// 
        /// </summary>
        public static bool TryCreate(ref Json5SyntaxNodes nodes, out Json5SyntaxNode node, out Json5SyntaxNodesReader reader, EJson5Token token = EJson5Token.Value)
        {
            var startPosition = nodes.Position;
            reader = new Json5SyntaxNodesReader();
            if (reader.TryRead(ref nodes, out node, token))
                return true;
            nodes.Position = startPosition;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public Json5SyntaxNode Read(ref Json5SyntaxNodes nodes, EJson5Token token = EJson5Token.Value)
        {
            return !TryRead(ref nodes, out var node, token) ? throw new ArgumentException() : node;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TryRead(ref Json5SyntaxNodes nodes, out Json5SyntaxNode node, EJson5Token token = EJson5Token.Value)
        {
            do
            {
                node = nodes.MoveNext(token | EJson5Token.ArrayOpen | EJson5Token.ObjectOpen | EJson5Token.Close);
                if (node.Token == EJson5Token.Close)
                {
                    if (BracketOpenCount == 0)
                    {
                        --nodes.Position;
                        break;
                    }

                    --BracketOpenCount;
                }
                else if (node.Token is EJson5Token.ArrayOpen or EJson5Token.ObjectOpen)
                    ++BracketOpenCount;

                if ((node.Token & token) != 0)
                    return true;
            } while (node.Token != EJson5Token.None && node.Token != EJson5Token.Close);

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close(ref Json5SyntaxNodes nodes)
        {
            while (BracketOpenCount > 0)
            {
                var node = nodes.MoveNext(EJson5Token.Close);
                if (node.Token == EJson5Token.None)
                    break;
                --BracketOpenCount;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Json5SyntaxNode
    {
        public IntRange SourceRange;
        public IntRange ContentRange;
        public EJson5Token Token;
        public string FullSource;
        public ReadOnlyMemory<char> ContentMem => FullSource.AsMemory(ContentRange);
        public string ContentCopyString => FullSource.Substring(ContentRange.Begin, ContentRange.End - ContentRange.Begin);
        public ReadOnlySpan<char> ContentSpan => FullSource.AsSpan(ContentRange.Begin, ContentRange.End - ContentRange.Begin);
        public ReadOnlyMemory<char> Source => FullSource.AsMemory(SourceRange);
        public int RemainingLength => FullSource.Length - SourceRange.End;
        public override string ToString() => $"[{Token}]{ContentMem}";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public void Parse(Json5DeserializeOptionData options)
        {
            while (RemainingLength > 0)
            {
                var c = FullSource[SourceRange.End++];
                if (char.IsWhiteSpace(c))
                    continue;
                ContentRange = SourceRange.End - 1;
                switch (c)
                {
                    case '{':
                        Token = EJson5Token.ObjectOpen;
                        return;
                    case '[':
                    case '【':
                        Token = EJson5Token.ArrayOpen;
                        return;
                    case '}':
                    case ']':
                    case '】':
                        Token = EJson5Token.Close;
                        return;
                    case ':':
                    case '：':
                    case '，':
                    case ',':
                        Token = EJson5Token.Skip;
                        return;
                    default:
                        if (RemainingLength > 0)
                        {
                            var nextChar = FullSource[SourceRange.End];
                            if ((c == '/' && nextChar == '/') || (c == '/' && nextChar == '*'))
                            {
                                ++SourceRange.End;
                                Token = EJson5Token.Comment;
                                ParseComment(nextChar);
                                return;
                            }
                        }

                        --SourceRange.End;
                        Token = EJson5Token.Value;
                        ParseValue();
                        return;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ParseComment(char commentType)
        {
            ContentRange.Begin = SourceRange.Begin + 2;
            while (RemainingLength > 0)
            {
                var c = FullSource[SourceRange.End++];
                if (commentType == '/' && c == '\n')
                {
                    ContentRange.End = SourceRange.End - 1;
                    if (ContentMem.Span[^1] == '\r')
                        --ContentRange.End;
                    break;
                }

                if (commentType != '*' || c != '*' || FullSource.ElementAtOrDefault(SourceRange.End) != '/')
                    continue;
                ContentRange.End = SourceRange.End - 1;
                ++SourceRange.End;
                break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ParseValue()
        {
            byte type = 0;
            var beginWhiteCharCount = 0;
            var endWhiteCharCount = -1;
            while (RemainingLength > 0)
            {
                var c = FullSource[SourceRange.End++];
                if (c == '\\' && RemainingLength > 0)
                {
                    ++SourceRange.End;
                    continue;
                }

                if (type == 0 && c is '{' or '}' or '[' or '【' or ']' or '】' or ':' or '：' or ',' or '，')
                {
                    --SourceRange.End;
                    break;
                }

                if (c == '\'')
                {
                    if (type == 0)
                    {
                        if (endWhiteCharCount < 0)
                            ++beginWhiteCharCount;
                        type = 1;
                    }
                    else if (type == 1)
                    {
                        endWhiteCharCount = Math.Max(1, endWhiteCharCount + 1);
                        break;
                    }
                }
                else if (c == '"')
                {
                    if (type == 0)
                    {
                        if (endWhiteCharCount < 0)
                            ++beginWhiteCharCount;
                        type = 2;
                    }
                    else if (type == 2)
                    {
                        endWhiteCharCount = Math.Max(1, endWhiteCharCount + 1);
                        break;
                    }
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (endWhiteCharCount < 0)
                        ++beginWhiteCharCount;
                    else
                        ++endWhiteCharCount;
                }
                else
                {
                    endWhiteCharCount = 0;
                }
            }

            if (beginWhiteCharCount > 0)
                ContentRange.Begin += beginWhiteCharCount;
            ContentRange.End = SourceRange.End - endWhiteCharCount;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum EJson5Token
    {
        None = 0,
        Value = 0x1,
        ArrayOpen = 0x2,
        ObjectOpen = 0x4,
        Close = 0x8,
        Comment = 0x10,
        Skip = 0x20,
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Json5Deserializer
    {
        /// <summary>
        /// 
        /// </summary>
        public static object ToValue(ref Json5SyntaxNodes nodes, Type toType, Json5DeserializeOptionData options)
        {
            var obj = TryCustomDeserialize(ref nodes, toType, in options);
            if (obj != null)
                return obj;
            var node = nodes.MoveNext(EJson5Token.Value | EJson5Token.ArrayOpen | EJson5Token.ObjectOpen);
            try
            {
                switch (node.Token)
                {
                    case EJson5Token.ObjectOpen:
                        obj = ToObject(ref nodes, toType, options);
                        break;
                    case EJson5Token.ArrayOpen:
                        obj = ToArray(ref nodes, toType, options);
                        break;
                    default:
                    {
                        if (toType == typeof(Json5AnyValue))
                            obj = new Json5AnyValue(ParseValue(typeof(object), ref options, node));
                        else
                            obj = ParseValue(toType, ref options, node);
                        break;
                    }
                }

                return obj;
            }
            catch (Exception e)
            {
                throw new Exception($"{toType} | {node} | {options}", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static object ParseValue(Type toType, ref Json5DeserializeOptionData options, in Json5SyntaxNode node)
        {
            if (toType.IsEnum)
            {
                if (node.ContentSpan.IsEmpty)
                    return toType.DefaultValue();
                return Enum.TryParse(toType, node.ContentCopyString, false, out var enumObj) ? enumObj! : Enum.Parse(toType, node.ContentCopyString.Replace('|', ','));
            }

            if (toType == typeof(object))
            {
                if (long.TryParse(node.ContentSpan, out var l))
                    return l;
                if (double.TryParse(node.ContentSpan, out var d))
                    return d;
            }

            try
            {
                var byNullableType = Nullable.GetUnderlyingType(toType);
                var str = node.ContentMem.ToString();
                return byNullableType == null ? Convert.ChangeType(str, toType) : Activator.CreateInstance(toType, Convert.ChangeType(str, byNullableType))!;
            }
            catch (Exception)
            {
                try
                {
                    if (toType == typeof(byte[]) && node.ContentSpan.Length % 4 == 0)
                        return Convert.FromBase64String(node.ContentCopyString);
                    if (toType == typeof(bool))
                        return node.ContentSpan[0] == '1';
                    if (!options.IsFallback)
                    {
                        options.IsFallback = true;
                        return Json5.Deserialize(node.ContentMem.ToString(), toType, options);
                    }
                }
                catch
                {
                    // ignored
                }

                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static object ToObject(ref Json5SyntaxNodes nodes, Type toType, Json5DeserializeOptionData options)
        {
            object obj = null!;
            IDictionary? dict = null;
            IJson5FieldDeserializable? customFieldDeserializer = null;
            if (toType == typeof(object))
            {
                obj = dict = new Dictionary<string, object>();
            }
            else if (toType == typeof(Json5AnyValue))
            {
                obj = dict = new Dictionary<string, Json5AnyValue>();
            }
            else if (!toType.IsStatic())
            {
                dict = (obj = TypeAssistant.New(toType)) as IDictionary;
                customFieldDeserializer = obj as IJson5FieldDeserializable;
            }

            var kvTypes = dict != null ? dict.GetType().GetGenericArguments() : new[] { typeof(string), null! };

            Json5SyntaxNode node = default;
            object? key = null;
            while (nodes.Count > 0 && node.Token != EJson5Token.Close)
            {
                node = nodes[0];
                if (key == null)
                {
                    if (node.Token == EJson5Token.Value)
                        key = ToValue(ref nodes, kvTypes[0], options);
                    else
                        nodes.MoveNext();
                }
                else
                {
                    if (node.Token > EJson5Token.ObjectOpen)
                    {
                        nodes.MoveNext();
                        continue;
                    }

                    if (dict != null)
                    {
                        dict[key] = ToValue(ref nodes, kvTypes[1], options);
                        key = null;
                    }
                    else
                    {
                        var fieldName = key.ToString()!.Trim();
                        try
                        {
                            if (customFieldDeserializer?.JsonDeserialize(fieldName, ref nodes, null, options).IsHooked != true)
                            {
                                var field = new FieldOrPropertyInfo(toType, fieldName,
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase, false);
                                if (field.IsEmpty && options.FieldNameFallback != null)
                                {
                                    var name = options.FieldNameFallback(fieldName);
                                    if (name != null)
                                        field = new FieldOrPropertyInfo(toType, name,
                                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase, false);
                                }

                                if (field.IsEmpty && !options.IsIgnoreMissingField)
                                    throw new Exception($"not found field: {toType}.{key}, {node}");
                                key = null;
                                if ((field.IsEmpty && options.IsIgnoreMissingField) || field.IsDefineAttribute<NonSerializedAttribute>())
                                {
                                    node = nodes.MoveNext(EJson5Token.Close | EJson5Token.Value | EJson5Token.ArrayOpen | EJson5Token.ObjectOpen);
                                    if (node.Token is EJson5Token.ArrayOpen or EJson5Token.ObjectOpen)
                                    {
                                        var bracket = 1;
                                        while (bracket > 0)
                                        {
                                            node = nodes.MoveNext(EJson5Token.Close | EJson5Token.ArrayOpen | EJson5Token.ObjectOpen);
                                            if (node.Token == EJson5Token.Close)
                                                --bracket;
                                            else
                                                ++bracket;
                                        }
                                    }

                                    node = default;
                                }
                                else
                                {
                                    object? val;
                                    if (obj is IJson5Deserializable deserializable)
                                    {
                                        var result = deserializable.JsonDeserialize(ref nodes, field.Field as object ?? field.Property, options);
                                        val = result.IsHooked ? result.Result : ToValue(ref nodes, field.Type, options);
                                    }
                                    else
                                    {
                                        val = ToValue(ref nodes, field.Type, options);
                                    }

                                    if (val != null)
                                        field.SetValue(obj, val);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error?.Write($"{toType}.{fieldName}\n{e}");
                            throw;
                        }
                    }
                }
            }

            return toType == typeof(Json5AnyValue) ? new Json5AnyValue(obj) : obj;
        }

        /// <summary>
        /// 
        /// </summary>
        public static object ToArray(ref Json5SyntaxNodes nodes, Type toType, Json5DeserializeOptionData options)
        {
            // list, array, collection
            byte typeCode = 0;
            var elType = toType;
            IList list;
            if (toType == typeof(object))
            {
                list = new List<object>();
            }
            else if (toType == typeof(Json5AnyValue))
            {
                list = new List<Json5AnyValue>();
                typeCode = 1;
            }
            else if (toType.IsArray)
            {
                elType = toType.GetElementType()!;
                list = (IList)TypeAssistant.New(typeof(List<>).MakeGenericType(elType));
            }
            else
            {
                elType = toType.GetGenericArguments()[0];
                if (typeof(IList).IsAssignableFrom(toType))
                {
                    typeCode = 2;
                    list = (IList)TypeAssistant.New(toType);
                }
                else
                {
                    typeCode = 3;
                    list = (IList)TypeAssistant.New(typeof(List<>).MakeGenericType(elType));
                }
            }

            while (nodes.Count > 0)
            {
                var node = nodes[0];
                if (node.Token == EJson5Token.Close)
                {
                    nodes.MoveNext();
                    break;
                }

                if (node.Token > EJson5Token.ObjectOpen)
                {
                    nodes.MoveNext();
                    continue;
                }

                var val = ToValue(ref nodes, elType, options);
                list.Add(val);
            }

            if (typeCode >= 2)
                return typeCode == 3 ? TypeAssistant.New(toType, list) : list;
            var result = Array.CreateInstance(elType, list.Count);
            list.CopyTo(result, 0);
            return typeCode == 1 ? new Json5AnyValue(result) : result;
        }

        /// <summary>
        /// 
        /// </summary>
        public static object? TryCustomDeserialize(ref Json5SyntaxNodes nodes, Type toType, in Json5DeserializeOptionData options)
        {
            Json5CustomDeserializeResult result = default;
            IJson5Deserializable deserializer = null!;
            if (Json5.CustomDeserializers?.TryGetValue(toType, out deserializer!) == true)
            {
                result = deserializer.JsonDeserialize(ref nodes, null, options);
            }
            else if (typeof(IJson5Deserializable).IsAssignableFrom(toType))
            {
                deserializer = (IJson5Deserializable)TypeAssistant.New(toType);
                result = deserializer.JsonDeserialize(ref nodes, null, options);
            }

            if (result.HookedType == 0)
                return null;
            if (result.Result == null && result.HookedType != 2)
                return deserializer;
            return result.Result;
        }
    }

    #endregion

    #region other

    /// <summary>
    /// 
    /// </summary>
    public sealed class Json5AnyValue : IEnumerable<object>
    {
        public object Raw;
        public Json5AnyValue(object raw) => Raw = raw;
        public Json5AnyValue[]? AsArray => Raw as Json5AnyValue[];
        public Json5AnyValue[] Array => (Json5AnyValue[])Raw;
        public int Count => AsArray?.Length ?? AsDict?.Count ?? 0;
        public Dictionary<string, Json5AnyValue>? AsDict => Raw as Dictionary<string, Json5AnyValue>;
        public Dictionary<string, Json5AnyValue> Dict => (Dictionary<string, Json5AnyValue>)Raw;
        public Json5AnyValue this[int index] => Get(index);
        public Json5AnyValue this[string key] => Get(key);
        public Json5AnyValue Get(int index) => AsArray![index];
        public Json5AnyValue Get(string key) => AsDict![key];
        public Json5AnyValue? TryGet(int index) => AsArray?.ElementAtOrDefault(index);
        public Json5AnyValue? TryGet(string key) => AsDict?.GetValueOrDefault(key);

        public bool TryGet(string key, out Json5AnyValue v)
        {
            v = null!;
            return AsDict?.TryGetValue(key, out v!) == true;
        }

        public bool Has(int index) => AsArray?.Length > index;
        public bool Has(string key) => AsDict?.ContainsKey(key) == true;
        public override string ToString() => Raw.ToString()!;
        public static implicit operator string(Json5AnyValue val) => Convert.ToString(val.Raw);
        public static implicit operator bool(Json5AnyValue val) => Convert.ToBoolean(val.Raw);
        public static implicit operator byte(Json5AnyValue val) => Convert.ToByte(val.Raw);
        public static implicit operator sbyte(Json5AnyValue val) => Convert.ToSByte(val.Raw);
        public static implicit operator short(Json5AnyValue val) => Convert.ToInt16(val.Raw);
        public static implicit operator ushort(Json5AnyValue val) => Convert.ToUInt16(val.Raw);
        public static implicit operator int(Json5AnyValue val) => Convert.ToInt32(val.Raw);
        public static implicit operator uint(Json5AnyValue val) => Convert.ToUInt32(val.Raw);
        public static implicit operator long(Json5AnyValue val) => Convert.ToInt64(val.Raw);
        public static implicit operator ulong(Json5AnyValue val) => Convert.ToUInt64(val.Raw);
        public static implicit operator float(Json5AnyValue val) => Convert.ToSingle(val.Raw);
        public static implicit operator double(Json5AnyValue val) => Convert.ToDouble(val.Raw);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<object> GetEnumerator() =>
#if NET8_0_OR_GREATER
            (AsArray as IEnumerable<object>)?.GetEnumerator()
#else
            AsArray?.Cast<object>().GetEnumerator()
#endif
            ?? AsDict?.Cast<object>().GetEnumerator() ?? Enumerable.Empty<object>().GetEnumerator();
    }

    #endregion
}