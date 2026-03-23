// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    [WorldComponentOption(options: EComponentOption.AlwaysReceiveDestroy)]
    public struct Mng<T> : IWorldAwake, IWorldDestroy
    {
        /// <summary>
        /// 略微感觉做法有点糙, 但又没想出是否要单独写个分页对象储存池,感觉好像又没太大必要, 暂时先这样实现
        /// </summary>
        [ThreadStatic] private static FixedIndexList<T> _objects;

        private int _index;

        public ref T Val => ref _objects.GetRef(_index - 1);

        public override string ToString() => Val.ToString();

        public void OnAwake(WorldCore world, WorldEntityId entityId)
        {
            Set(default);
        }

        public void OnDestroy(WorldCore world, WorldEntityId entityId)
        {
            if (_index == 0) return;
            _objects.RemoveAt(_index - 1);
            _index = 0;
        }

        public void Set(in T val)
        {
            if (_index == 0)
                _index = _objects.Add(val) + 1;
            else
                _objects.GetRef(_index - 1) = val;
        }

        public static implicit operator T(Mng<T> mng) => _objects[mng._index - 1];
    }
}