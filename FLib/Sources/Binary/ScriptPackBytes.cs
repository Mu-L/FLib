// ==================== qcbf@qq.com | 2026-03-27 ====================

#nullable enable
using System;

namespace FLib
{
    /// <summary>
    /// 
    /// </summary>
    public struct ScriptPackBytes : IJson5Serializable, IJson5Deserializable, IBytesSerializable, IBytesPackable
    {
        /// <summary>
        /// 
        /// </summary>
        public byte[] Bytes;
        
        /// <summary>
        /// 
        /// </summary>
        public readonly string TypeName => new BytesReader(Bytes).ReadString();
        
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
        
        public ScriptPackBytes(IBytesPackable instance)
        {
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
        
        /// <summary>
        /// 
        /// </summary>
        public readonly IBytesPackable? CreateInstance()
        {
            if (Bytes?.Length > 0)
            {
                var instance = (IBytesPackable)TypeAssistant.New(TypeName);
                BytesPack.Unpack(ref instance, InstanceBytes.Span);
                return instance;
            }
            
            return null;
        }
        
        #region serialization
        
        public readonly string JsonSerialize(object serializeObject, object? customData, int indent, Json5SerializeOptionData opData)
        {
            return ScriptPackInstance.JsonSerializeImpl(CreateInstance());
        }
        
        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object? otherData, in Json5DeserializeOptionData options)
        {
            if (ScriptPackInstance.JsonDeserializeImpl(ref nodes, out var instance) && instance != null)
            {
                this = new ScriptPackBytes(instance);
                return true;
            }
            
            return false;
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
}