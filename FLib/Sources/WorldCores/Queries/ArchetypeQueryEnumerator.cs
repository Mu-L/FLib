// ==================== qcbf@qq.com | 2026-01-18 ====================

using System.Collections;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    public struct ArchetypeQueryEnumerator : IEnumerator<Archetype>
    {
        private readonly Archetype[] _archetypes;
        private int _index;
        public Archetype Current => _archetypes[_index];
        object IEnumerator.Current => Current;

        public ArchetypeQueryEnumerator(Archetype[] archetypes)
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