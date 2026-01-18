// ==================== qcbf@qq.com | 2026-01-05 ====================

using System;
using System.Buffers;
using System.Diagnostics;

namespace FLib.WorldCores
{
    public struct ComponentSparseList
    {
        public int[] List;

        public int Length => List.Length;

        public int this[IncrementId componentId]
        {
            get => Get(componentId);
            set => List[componentId] = value;
        }

        public ComponentSparseList(int[] source, bool isPooledArray) : this(isPooledArray ? ArrayPool<int>.Shared.Rent(source.Length) : new int[source.Length])
        {
            source.CopyTo(List, 0);
        }

        public ComponentSparseList(IncrementId maxId, bool isPooledArray) : this(isPooledArray ? ArrayPool<int>.Shared.Rent(maxId.Raw) : new int[maxId.Raw])
        {
        }

        public ComponentSparseList(int[] list)
        {
            List = list;
            Array.Fill(List, -1);
        }

        public void ResizeOnPool(IncrementId newMaxId)
        {
            if (newMaxId.IsEmpty)
            {
                ArrayPool<int>.Shared.Return(List);
                List = null;
                return;
            }

            if (newMaxId.Raw <= List.Length) return;
            var pool = ArrayPool<int>.Shared;
            var list = pool.Rent(newMaxId.Raw);
            try
            {
                Array.Copy(List, list, List.Length);
            }
            catch
            {
                pool.Return(list);
                throw;
            }

            pool.Return(List);
            List = list;
        }

        public bool TryGet(Type type, out int index)
        {
            return TryGet(ComponentRegistry.GetId(type), out index);
        }

        public bool TryGet<T>(out int index)
        {
            return TryGet(ComponentRegistry.GetId<T>(), out index);
        }

        public bool TryGet(IncrementId componentId, out int index)
        {
            if (List.Length < componentId)
            {
                index = -1;
                return false;
            }

            return (index = List[componentId]) >= 0;
        }

        public int Get(Type type) => Get(ComponentRegistry.GetId(type));
        public int Get<T>() => Get(ComponentRegistry.GetId<T>());

        public int Get(IncrementId componentId)
        {
            Debug.Assert(Has(componentId), "not found component");
            return List[componentId];
        }

        public void ValidateSet(IncrementId componentId, int value)
        {
            Debug.Assert(Has(componentId), "not found component");
            List[componentId] = value;
        }

        public int GetAndClear(IncrementId componentId)
        {
            var temp = this[componentId];
            this[componentId] = -1;
            return temp;
        }

        public bool Has<T>() => Has(ComponentRegistry.GetId<T>());
        public bool Has(IncrementId componentId) => componentId.Raw <= List.Length && List[componentId] >= 0;
    }
}