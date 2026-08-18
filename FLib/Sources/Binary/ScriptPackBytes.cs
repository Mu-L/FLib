// ==================== qcbf@qq.com | 2026-03-27 ====================

#nullable enable
using System;
using System.Text;

namespace FLib
{
    /// <summary>
    /// 
    /// </summary>
    public struct ScriptPackBytes : IJson5Serializable, IJson5Deserializable, IBytesSerializable, IBytesPackable, IScriptPackable
    {
        /// <summary>
        /// 
        /// </summary>
        public byte[] Bytes;

        /// <summary>  </summary>
        public readonly Type? ScriptType => TypeAssistant.GetType(ScriptTypeName, isThrowOnError: false);

        /// <summary>  </summary>
        public readonly string ScriptTypeName => IsEmpty ? string.Empty : new BytesReader(Bytes).ReadString();

        /// <summary>
        /// 
        /// </summary>
        public readonly Type ScriptBaseType => typeof(IBytesPackable);

        /// <summary>
        /// 
        /// </summary>
        public readonly bool IsEmpty => Bytes == null || Bytes.Length == 0;

        /// <summary>
        /// 
        /// </summary>
        public readonly Memory<byte> InstanceBytes
        {
            get
            {
                var reader = (BytesReader)Bytes;
                var size = reader.ReadLength();
                return Bytes.AsMemory(reader.Position + size);
            }
        }

        public ScriptPackBytes(byte[] bytes)
        {
            Bytes = bytes;
        }

        public ScriptPackBytes(IBytesPackable? instance)
        {
            Bytes = Array.Empty<byte>();
            SetInstance(instance);
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly IBytesPackable? CreateInstance()
        {
            if (Bytes?.Length > 0)
            {
                var instance = (IBytesPackable)TypeAssistant.New(ScriptTypeName);
                BytesPack.Unpack(ref instance, InstanceBytes.Span);
                return instance;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetInstance(IBytesPackable? instance)
        {
            if (instance == null)
            {
                Bytes = Array.Empty<byte>();
                return;
            }

            var writer = new BytesWriter() { Allocator = BytesWriter.PoolAllocator };
            try
            {
                writer.Push(TypeAssistant.GetTypeName(instance.GetType()));
                BytesPack.Pack(instance, ref writer);
                Bytes = writer.Span.ToArray();
            }
            finally
            {
                writer.TryReleasePoolAllocator();
            }
        }

        public override string ToString() => $"{ScriptTypeName}[{IOUtility.FormatSize((Bytes?.Length).GetValueOrDefault())}]";

        #region serialization

        public readonly bool JsonSerialize(StringBuilder jsonText, object serializeObject, object? customData, int indent, Json5SerializeOptionData opData)
        {
            ScriptPackInstance.JsonSerializeImpl(jsonText, CreateInstance(), 0);
            return true;
        }

        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object? otherData, in Json5DeserializeOptionData options)
        {
            var r = ScriptPackInstance.JsonDeserializeImpl(ref nodes, out var inst, null);
            if (r.IsHooked)
                this = new ScriptPackBytes(inst);
            return r;
        }

        public readonly void Z_BytesWrite(ref BytesWriter writer)
        {
            writer.Push(Bytes);
        }

        public void Z_BytesRead(ref BytesReader reader)
        {
            Bytes = reader.ReadArray<byte>();
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


    /// <summary>
    /// 
    /// </summary>
    public struct ScriptPackBytes<T> : IJson5Serializable, IJson5Deserializable, IBytesSerializable, IBytesPackable, IScriptPackable where T : IBytesPackable
    {
        public byte[] Bytes;
        public readonly bool IsEmpty => Bytes == null || Bytes.Length == 0;
        public readonly string ScriptTypeName => IsEmpty ? string.Empty : new BytesReader(Bytes).ReadString();
        public readonly Type? ScriptType => TypeAssistant.GetType(ScriptTypeName, isThrowOnError: false);
        public readonly Type ScriptBaseType => typeof(T);
        public readonly Memory<byte> InstanceBytes => new ScriptPackBytes(Bytes).InstanceBytes;
        public ScriptPackBytes(byte[] bytes) => Bytes = bytes;

        public ScriptPackBytes(IBytesPackable instance)
        {
            Bytes = Array.Empty<byte>();
            SetInstance(instance);
        }

        IBytesPackable? IScriptPackable.CreateInstance() => CreateInstance();
        public readonly T? CreateInstance() => (T?)new ScriptPackBytes(Bytes).CreateInstance();
        public void SetInstance(IBytesPackable? instance) => Bytes = new ScriptPackBytes(instance).Bytes;
        public override string ToString() => $"{ScriptTypeName}[{IOUtility.FormatSize((Bytes?.Length).GetValueOrDefault())}]";
        public static implicit operator ScriptPackBytes(ScriptPackBytes<T> v) => new(v.Bytes);

        #region serialization

        public readonly bool JsonSerialize(StringBuilder jsonText, object serializeObject, object? customData, int indent, Json5SerializeOptionData opData)
        {
            ScriptPackInstance.JsonSerializeImpl(jsonText, CreateInstance(), typeof(T).Namespace?.Length ?? 0);
            return true;
        }

        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object? otherData, in Json5DeserializeOptionData options)
        {
            var r = ScriptPackInstance.JsonDeserializeImpl(ref nodes, out var inst, typeof(T).Namespace);
            if (r.IsHooked)
                this = new ScriptPackBytes<T>(inst);
            return r;
        }

        public readonly void Z_BytesWrite(ref BytesWriter writer) => writer.Push(Bytes);
        public void Z_BytesRead(ref BytesReader reader) => Bytes = reader.ReadArray<byte>();

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