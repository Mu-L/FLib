// ==================== qcbf@qq.com | 2026-03-27 ====================

#nullable enable
using System;
using System.Buffers;

namespace FLib
{
    public struct ScriptPackInstance : IJson5Serializable, IJson5Deserializable, IBytesSerializable, IBytesPackable
    {
        [ThreadStatic] private static object[]? _jsonSerializeArray;
        public IBytesPackable? Instance;
        
        public ScriptPackInstance(IBytesPackable instance) => Instance = instance;
        
        #region serialization
        
        public readonly string JsonSerialize(object serializeObject, object? customData, int indent, Json5SerializeOptionData opData)
        {
            return JsonSerializeImpl(Instance);
        }
        
        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object? otherData, in Json5DeserializeOptionData options)
        {
            return JsonDeserializeImpl(ref nodes, out Instance);
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
        
        internal static Json5CustomDeserializeResult JsonDeserializeImpl(ref Json5SyntaxNodes nodes, out IBytesPackable? instance)
        {
            if (!Json5SyntaxNodesReader.TryCreate(ref nodes, out var node, out var reader))
            {
                instance = null;
                return false;
            }
            
            var type = TypeAssistant.GetType(node.ContentCopyString);
            instance = (IBytesPackable)nodes.To(type);
            reader.Close(ref nodes);
            return true;
        }
        
        internal static string JsonSerializeImpl(IBytesPackable? instance)
        {
            string json;
            if (instance == null)
            {
                json = "[]";
            }
            else
            {
                _jsonSerializeArray ??= new object[2];
                _jsonSerializeArray[0] = TypeAssistant.GetTypeName(instance.GetType());
                _jsonSerializeArray[1] = instance;
                json = Json5.Serialize(_jsonSerializeArray);
            }
            
            return json;
        }
        
        #endregion
    }
}