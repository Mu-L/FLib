// ==================== qcbf@qq.com | 2026-04-30 ====================

using System.Collections;
using System.Collections.Generic;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    /// <summary>
    /// 组件迭代器
    /// </summary>
    public struct WorldComponentEnumerator<T> : IEnumerator<T>
    {
        public readonly WorldSoaComponentGroup<T> Group;
        private int _index;
        public readonly ref T Current => ref Group[_index];
        public readonly WorldEntityId Entity => Group.ComponentEntities[_index];
        readonly T IEnumerator<T>.Current => Group[_index];
        readonly object IEnumerator.Current => Current;

        public WorldComponentEnumerator(WorldSoaComponentGroup<T> group)
        {
            Group = group;
            _index = -1;
        }

        public bool MoveNext()
        {
            if (Group == null)
                return false;
            while (++_index < Group.Count && !Group.ComponentEntities[_index].IsEmpty)
            {
            }

            return _index < Group.Count;
        }

        public void Reset() => _index = -1;
        public void Dispose() => Reset();
    }
}