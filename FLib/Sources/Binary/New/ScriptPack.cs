// ==================== qcbf@qq.com | 2026-03-27 ====================

using System;
using System.Buffers;

namespace FLib.New
{
    public struct ScriptPack : IJson5Serializable, IJson5Deserializable, IBytesSerializable, IBytesPackable
    {
        [ThreadStatic] private static object[] _jsonSerializeArray;
        public IBytesPackable Instance;
        
        public ScriptPack(IBytesPackable instance) => Instance = instance;
        
        #region serialization
        
        public string JsonSerialize(object serializeObject, object customData, int indent, Json5SerializeOptionData opData)
        {
            string json;
            if (Instance == null)
            {
                json = "[]";
            }
            else
            {
                _jsonSerializeArray ??= new object[2];
                _jsonSerializeArray[0] = TypeAssistant.GetTypeName(Instance.GetType());
                _jsonSerializeArray[1] = Instance;
                json = Json5.Serialize(_jsonSerializeArray);
            }
            
            return json;
        }
        
        public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
        {
            if (!Json5SyntaxNodesReader.TryCreate(ref nodes, out var node, out var reader))
                return false;
            var type = TypeAssistant.GetType(node.ContentCopyString);
            Instance = (IBytesPackable)nodes.To(type);
            reader.Close(ref nodes);
            return true;
        }
        
        public void Z_BytesWrite(ref BytesWriter writer)
        {
            writer.PushScript(Instance);
        }
        
        public void Z_BytesRead(ref BytesReader reader)
        {
            Instance = reader.ReadScript();
        }
        
        public void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer)
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