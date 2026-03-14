// ==================== qcbf@qq.com | 2025-08-08 ====================

using System.Collections;
using System.Collections.Generic;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public static class TransformChildEnumerator
    {
        public enum EType : byte
        {
            Active,
            Inactive,
            Any,
        }

        public struct Enumerator<T> : IEnumerator<T>, IEnumerable<T> where T : Component
        {
            public Transform Root;
            public int Index;
            public int Count;
            public EType Type;
            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                while (++Index < Count)
                {
                    Current = Root.GetChild(Index).GetComponent<T>();
                    if (Current == null || (Type != EType.Any && (Type == EType.Active) != Current.gameObject.activeSelf))
                        continue;
                    return true;
                }
                return false;
            }

            public void Reset() => Index = -1;
            public void Dispose() { }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public IEnumerator<T> GetEnumerator() => new Enumerator<T>() { Root = Root, Index = -1, Count = Count, Type = Type };
        }

        public static Enumerator<T> Children<T>(this Transform root, EType type = EType.Active) where T : Component => new() { Root = root, Count = root.childCount, Index = -1, Type = type };
    }
}
