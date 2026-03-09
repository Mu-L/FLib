// ==================== qcbf@qq.com | 2026-01-09 ====================

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public struct WorldChunkQueryEnumerator : IEnumerator<WorldChunk>
    {
        private readonly WorldQuerySharedComponent[] _sharedComponents;
        private WorldArchetypeQueryEnumerator _archetypeEnumerator;
        private HashSet<WorldChunk>.Enumerator _chunkEnumerator;
        private bool _initialized;
        public WorldChunk Current => _chunkEnumerator.Current;
        object IEnumerator.Current => Current;

        /// <summary>
        /// 
        /// </summary>
        public WorldChunkQueryEnumerator(in WorldQueryFilter filter)
        {
            _sharedComponents = filter.SharedComponents;
            _archetypeEnumerator = new WorldArchetypeQueryEnumerator(filter.Archetypes);
            _chunkEnumerator = default;
            _initialized = false;
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool MoveNext()
        {
            if (!_initialized)
            {
                _initialized = true;
                if (!_archetypeEnumerator.MoveNext())
                    return false;
                _chunkEnumerator = _archetypeEnumerator.Current!.AllChunks.GetEnumerator();
            }

            if (MoveNextChunk()) return true;

            while (_archetypeEnumerator.MoveNext())
            {
                _chunkEnumerator = _archetypeEnumerator.Current!.AllChunks.GetEnumerator();
                if (MoveNextChunk())
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _chunkEnumerator.Dispose();
            _archetypeEnumerator.Dispose();
        }

        public void Reset()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool MoveNextChunk()
        {
            while (_chunkEnumerator.MoveNext())
            {
                var chunk = _chunkEnumerator.Current!;
                for (var i = 0; i < _sharedComponents.Length; i++)
                {
                    var sharedComponent = _sharedComponents[i];
                    if (!chunk.Has(sharedComponent.ComponentId, sharedComponent.Hash))
                        goto ContinueWhile;
                }

                return true;
                ContinueWhile: ;
            }

            return false;
        }
    }
}