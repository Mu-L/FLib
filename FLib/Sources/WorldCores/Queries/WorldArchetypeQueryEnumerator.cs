// ==================== qcbf@qq.com | 2026-01-18 ====================

using System.Collections;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    public struct WorldArchetypeQueryEnumerator : IEnumerator<WorldArchetype>
    {
        private readonly WorldArchetype[] _archetypes;
        private int _index;
        public WorldArchetype Current => _archetypes[_index];
        object IEnumerator.Current => Current;

        public WorldArchetypeQueryEnumerator(WorldArchetype[] archetypes)
        {
            _archetypes = archetypes;
            _index = -1;
        }

        public bool MoveNext() => ++_index < _archetypes.Length;
        public void Reset() => _index = -1;

        public void Dispose()
        {
        }
    }
}