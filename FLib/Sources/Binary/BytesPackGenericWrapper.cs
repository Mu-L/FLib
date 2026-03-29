// ==================== qcbf@qq.com | 2026-03-29 ====================

using System;
using System.Runtime.CompilerServices;

namespace FLib
{
    public interface IBytesPackGenericWrapper
    {
        public void Deserialize(ref byte data, ref BytesReader reader);
        public void Deserialize(ref byte data, in ReadOnlySpan<byte> bytes);
        public void Serialize(ref byte data, ref BytesWriter writer);
    }
    
    public class BytesPackGenericWrapper<T> : IBytesPackGenericWrapper where T : IBytesPackable
    {
        public void Deserialize(ref byte data, in ReadOnlySpan<byte> bytes)
        {
            ref var comp = ref Unsafe.As<byte, T>(ref data);
            BytesPack.Unpack(ref comp, bytes);
        }
        
        public void Deserialize(ref byte data, ref BytesReader reader)
        {
            ref var comp = ref Unsafe.As<byte, T>(ref data);
            BytesPack.Unpack(ref comp, ref reader);
        }
        
        public void Serialize(ref byte data, ref BytesWriter writer)
        {
            ref readonly var comp = ref Unsafe.As<byte, T>(ref data);
            BytesPack.Pack(comp, ref writer);
        }
    }
}