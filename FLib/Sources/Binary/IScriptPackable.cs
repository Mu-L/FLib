// ==================== qcbf@qq.com | 2026-04-11 ====================

#nullable enable

using System;

namespace FLib
{
    public interface IScriptPackable
    {
        public Type? ScriptType { get; }
        public Type ScriptBaseType { get; }
        public void SetInstance(IBytesPackable? instance);
        public IBytesPackable? CreateInstance();
    }
}