// ==================== qcbf@qq.com | 2025-10-24 ====================

using System;
using FLib;

namespace FLib
{
    /// <summary>
    /// 对事件监听的托管
    /// </summary>
    public struct FEventListenManaged : IDisposable
    {
        private ListenData _one;
        private PooledList<ListenData> _more;

        public readonly bool IsEmpty => _one.IsEmpty && _more.IsEmpty;

        /// <summary>
        /// 
        /// </summary>
        public readonly struct ListenData
        {
            public readonly int EvtId;
            public readonly FEvent Evt;
            public readonly Delegate Handler;

            public ListenData(FEvent evt, int evtId, Delegate handler)
            {
                Evt = evt;
                EvtId = evtId;
                Handler = handler;
            }

            public bool IsEmpty => Handler == null;
            public void Unlisten() => Evt?.UnlistenEventImpl(EvtId, Handler);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Add(FEvent evt, int evtId, Delegate handler)
        {
            var data = new ListenData(evt, evtId, handler);
            if (_one.IsEmpty)
                _one = data;
            else
                _more.Add(data);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (_one.IsEmpty)
                return;

            _one.Unlisten();
            _one = default;

            if (!_more.IsInitialized) return;
            try
            {
                for (var i = 0; i < _more.Count; i++)
                    _more[i].Unlisten();
            }
            finally
            {
                _more.Dispose();
                _more = default;
            }
        }
    }
}