// ==================== qcbf@qq.com | 2026-03-27 ====================

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLib
{
    public struct ScriptPackInstance : IJson5Serializable, IJson5Deserializable, IBytesSerializable, IBytesPackable, IEquatable<ScriptPackInstance>, IScriptPackable
    {
        private const string ScriptTypeJsonKey = "$type";
        public IBytesPackable Instance;
        public bool IsEmpty => Instance == null;
        public Type ScriptType => Instance?.GetType();

        public Type ScriptBaseType => typeof(IBytesPackable);

        public ScriptPackInstance(IBytesPackable instance) => Instance = instance;
        public void SetInstance(IBytesPackable instance) => Instance = instance;
        public IBytesPackable CreateInstance() => Instance;

        public bool Equals(ScriptPackInstance other) => EqualityComparer<object>.Default.Equals(Instance, other.Instance);
        public override bool Equals(object obj) => obj is ScriptPackInstance other && Equals(other);
        public override int GetHashCode() => EqualityComparer<object>.Default.GetHashCode(Instance);

        #region serialization

        public readonly string JsonSerialize(object serializeObject, object customData, int indent, Json5SerializeOptionData opData)
        {
            return JsonSerializeImpl(Instance, 0);
        }

        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
        {
            return JsonDeserializeImpl(ref nodes, out Instance!, null);
        }

        public readonly void Z_BytesWrite(ref BytesWriter writer)
        {
            writer.PushScript(Instance);
        }

        public void Z_BytesRead(ref BytesReader reader)
        {
            Instance = reader.ReadScript();
        }

        public readonly void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer)
        {
            key.Push(ref writer, 1);
            Z_BytesWrite(ref writer);
        }

        public void Z_BytesPackRead(int key, ref BytesReader reader)
        {
            if (key == 1)
                Z_BytesRead(ref reader);
        }

        internal static Json5CustomDeserializeResult JsonDeserializeImpl(ref Json5SyntaxNodes nodes, out IBytesPackable instance, string ns)
        {
            if (!Json5SyntaxNodesReader.TryCreate(ref nodes, out var node, out var reader))
            {
                instance = null;
                return false;
            }

            if (!node.ContentSpan.SequenceEqual(ScriptTypeJsonKey))
                throw new NotSupportedException(node.ContentCopyString);
            var typeName = reader.Read(ref nodes).ContentCopyString;
            if (ns != null)
            {
                typeName = StringFLibUtility.ReleaseStrBufAndResult(StringFLibUtility.GetStrBuf(typeName.Length + ns.Length + 1)
                    .Append(ns).Append('.').Append(typeName));
            }

            nodes.Nodes[--nodes.Position].Token = EJson5Token.ObjectOpen;
            instance = (IBytesPackable)nodes.To(TypeAssistant.GetType(typeName));
            reader.Close(ref nodes);
            return true;
        }

        internal static string JsonSerializeImpl(IBytesPackable instance, int nsLen)
        {
            string json;
            if (instance == null)
            {
                json = "{}";
            }
            else
            {
                var strbuf = new StringBuilder(64);
                Json5Serializer.PushDictKey(ScriptTypeJsonKey, strbuf, 0, default);
                var typeName = TypeAssistant.GetTypeName(instance.GetType());
                if (nsLen > 0)
                    typeName = typeName[(nsLen + 1)..];
                Json5Serializer.PushValue(typeName, strbuf, 0, default);
                strbuf.Append(',').Append(' ');
                var pos = strbuf.Length;
                Json5Serializer.PushValue(instance, strbuf, 0, default);
                for (var i = pos; i > 0; i--)
                    strbuf[i] = strbuf[i - 1];
                strbuf[0] = '{';
                json = strbuf.ToString();
            }

            return json;
        }

        #endregion
    }

    public struct ScriptPackInstance<T> : IJson5Serializable, IJson5Deserializable, IBytesSerializable, IBytesPackable, IScriptPackable, IEquatable<ScriptPackInstance<T>>
        where T : IBytesPackable
    {
        public T Instance;

        public ScriptPackInstance(T instance) => Instance = instance;

        public bool IsEmpty => Instance == null;
        public Type ScriptType => Instance?.GetType();

        public Type ScriptBaseType => typeof(T);
        public void SetInstance(IBytesPackable instance) => Instance = (T)instance;
        public IBytesPackable CreateInstance() => Instance;

        public static implicit operator T(in ScriptPackInstance<T> v) => v.Instance;
        public static implicit operator ScriptPackInstance<T>(T v) => new(v);
        public bool Equals(ScriptPackInstance<T> other) => EqualityComparer<T>.Default.Equals(Instance, other.Instance);
        public override bool Equals(object obj) => obj is ScriptPackInstance<T> other && Equals(other);
        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Instance);

        #region serialization

        public readonly string JsonSerialize(object serializeObject, object customData, int indent, Json5SerializeOptionData opData)
        {
            return ScriptPackInstance.JsonSerializeImpl(Instance, typeof(T).Namespace?.Length ?? 0);
        }

        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
        {
            var r = ScriptPackInstance.JsonDeserializeImpl(ref nodes, out var inst, typeof(T).Namespace);
            if (r.IsHooked)
                Instance = (T)inst;
            return r;
        }

        public readonly void Z_BytesWrite(ref BytesWriter writer)
        {
            writer.PushScript(Instance);
        }

        public void Z_BytesRead(ref BytesReader reader)
        {
            Instance = (T)reader.ReadScript();
        }

        public readonly void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer)
        {
            key.Push(ref writer, 1);
            Z_BytesWrite(ref writer);
        }

        public void Z_BytesPackRead(int key, ref BytesReader reader)
        {
            if (key == 1)
                Z_BytesRead(ref reader);
        }

        #endregion
    }
}