// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib
{
    /// <summary>
    /// 
    /// </summary>
    public class FEvent
    {
        public IDictionary<int, List2<FEventListenData>> AllListens;
        public List2<(int, FEventListenData)> PendingAddListens;
        private bool _dirtyRemoved;
        private byte _isDispatching;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>是否继续执行事件</returns>
        /// <typeparam name="T"></typeparam>
        public delegate bool PreEventHandler<T>(object dispatcher, ref T value);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public delegate void PostEventHandler<T>(object dispatcher, in T value);

        /// <summary>
        /// 事件监听处理程序异常
        /// </summary>
        protected virtual void ThrowEventError(Exception ex, in FEventListenData listenData)
        {
            Log.Error?.Write($"dispatch event error: {listenData.Handler}\n{ex}");
        }

        /// <summary>
        ///
        /// </summary>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public virtual void DispatchEvent<T>(in T evtData, object dispatcher = null) => DispatchEventById(TypeId<T>.Id, evtData, dispatcher);

        /// <summary>
        /// 
        /// </summary>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public virtual void DispatchEventById(int evtId, object dispatcher = null) => DispatchEventById<object>(evtId, null, dispatcher);

        /// <summary>
        ///
        /// </summary>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public virtual void DispatchEventById<T>(int evtId, in T evtData, object dispatcher = null)
        {
            if (AllListens == null || !AllListens.TryGetValue(evtId, out var list)) return;
            ++_isDispatching;
            try
            {
                var finalDispatcher = dispatcher ?? this;
                for (var i = 0; i < list.Count; i++)
                {
                    ref var listenData = ref list.GetValueRef(i);
                    var handler = listenData.Handler;
                    if (handler == null) // removed
                        continue;

                    if (listenData.IsListenOnce)
                    {
                        listenData.Handler = null;
                        _dirtyRemoved = true;
                    }

                    try
                    {
                        if (handler is PostEventHandler<T> func)
                            func(finalDispatcher, evtData);
#if DEBUG
                        else if (handler.GetType().GetGenericTypeDefinition() == typeof(PostEventHandler<>))
                            Log.Error?.Write($"event handler type error {handler.Target?.GetType().Name}.{handler.Method.Name} {typeof(T)}");
#endif
                    }
                    catch (Exception ex)
                    {
                        ThrowEventError(ex, new FEventListenData { Handler = handler, IsListenOnce = listenData.IsListenOnce, Priority = listenData.Priority });
                    }
                }
            }
            finally
            {
                ProcessDispatchComplete();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>is continuing run</returns>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public virtual bool DispatchPreEvent<T>(ref T evtData, object dispatcher = null) => DispatchPreEventById(TypeId<T>.Id, ref evtData, dispatcher);

        /// <summary>
        ///
        /// </summary>
        /// <returns>is continuing run</returns>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public virtual bool DispatchPreEventById(int evtId, object dispatcher = null)
        {
            object temp = null;
            return DispatchPreEventById(evtId, ref temp, dispatcher);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>is continuing run</returns>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public virtual bool DispatchPreEventById<T>(int evtId, ref T evtData, object dispatcher = null)
        {
            if (AllListens == null || !AllListens.TryGetValue(evtId, out var list)) return true;
            ++_isDispatching;
            try
            {
                var finalDispatcher = dispatcher ?? this;
                for (var i = 0; i < list.Count; i++)
                {
                    ref var listenData = ref list.GetValueRef(i);
                    var handler = listenData.Handler;
                    if (handler == null) // removed
                        continue;

                    if (listenData.IsListenOnce)
                    {
                        listenData.Handler = null;
                        _dirtyRemoved = true;
                    }

                    if (handler is PreEventHandler<T> func)
                    {
                        try
                        {
                            if (!func(finalDispatcher, ref evtData))
                                return false;
                        }
                        catch (Exception ex)
                        {
                            ThrowEventError(ex, new FEventListenData { Handler = handler, IsListenOnce = listenData.IsListenOnce, Priority = listenData.Priority });
                        }
                    }
#if DEBUG
                    else if (handler.GetType().GetGenericTypeDefinition() == typeof(PreEventHandler<>))
                    {
                        Log.Error?.Write($"event handler type error {handler.Target?.GetType().Name}.{handler.Method.Name} {typeof(T)}");
                    }
#endif
                }
            }
            finally
            {
                ProcessDispatchComplete();
            }

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void ProcessDispatchComplete()
        {
            if (--_isDispatching > 0)
                return;

            if (_dirtyRemoved)
            {
                foreach (var item in AllListens)
                    item.Value.RemoveAll(static v => v.Handler == null);
                _dirtyRemoved = false;
            }

            if (PendingAddListens != null)
            {
                try
                {
                    for (var i = 0; i < PendingAddListens.Count; i++)
                    {
                        ref readonly var item = ref PendingAddListens.GetValueRef(i);
                        ListenEventImpl(item.Item1, item.Item2.Handler, item.Item2.Priority, item.Item2.IsListenOnce);
                    }
                }
                finally
                {
                    PendingAddListens.Clear();
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected virtual List2<FEventListenData> GetListenEventList(Type t) => GetListenEventList(t.GetHashCode());

        /// <summary>
        ///
        /// </summary>
        protected virtual List2<FEventListenData> GetListenEventList(int evtId)
        {
            if (!AllListens.TryGetValue(evtId, out var list))
                AllListens.Add(evtId, list = new List2<FEventListenData>());
            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        public FEventListenHelper ListenEvent(int evtId, PostEventHandler<object> handler, short priority = 0, bool isListenOnce = false)
        {
            ListenEventImpl(evtId, handler, priority, isListenOnce);
            return new FEventListenHelper(this, evtId, handler);
        }

        /// <summary>
        /// 
        /// </summary>
        public FEventListenHelper ListenEvent<T>(PostEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
        {
            var id = TypeId<T>.Id;
            ListenEventImpl(id, handler, priority, isListenOnce);
            return new FEventListenHelper(this, id, handler);
        }

        /// <summary>
        ///
        /// </summary>
        public FEventListenHelper ListenEvent<T>(int evtId, PostEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
        {
            ListenEventImpl(evtId, handler, priority, isListenOnce);
            return new FEventListenHelper(this, evtId, handler);
        }

        /// <summary>
        /// 
        /// </summary>
        public FEventListenHelper ListenPreEvent(int evtId, PreEventHandler<object> handler, short priority = 0, bool isListenOnce = false)
        {
            ListenEventImpl(evtId, handler, priority, isListenOnce);
            return new FEventListenHelper(this, evtId, handler);
        }

        /// <summary>
        ///
        /// </summary>
        public FEventListenHelper ListenPreEvent<T>(PreEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
        {
            var id = TypeId<T>.Id;
            ListenEventImpl(id, handler, priority, isListenOnce);
            return new FEventListenHelper(this, id, handler);
        }

        /// <summary>
        ///
        /// </summary>
        public FEventListenHelper ListenPreEvent<T>(int evtId, PreEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
        {
            ListenEventImpl(evtId, handler, priority, isListenOnce);
            return new FEventListenHelper(this, evtId, handler);
        }

        /// <summary>
        ///
        /// </summary>
        protected virtual void ListenEventImpl(int evtId, Delegate handler, short priority, bool isListenOnce)
        {
            var listenData = new FEventListenData { Handler = handler, IsListenOnce = isListenOnce, Priority = priority };
            if (_isDispatching > 0)
            {
                (PendingAddListens ??= new List2<( int, FEventListenData)>()).Add((evtId, listenData));
                return;
            }

            AllListens ??= new Dictionary<int, List2<FEventListenData>>();
            var list = GetListenEventList(evtId);
            var index = list.Count;
            for (; index > 0; index--)
            {
                if (list.GetValueRef(index - 1).Priority >= priority)
                    break;
            }

            list.Insert(index, listenData);
        }

        /// <summary>
        ///
        /// </summary>
        public void UnlistenEvent<T>(PostEventHandler<T> handler) => UnlistenEventImpl(TypeId<T>.Id, handler);

        /// <summary>
        ///
        /// </summary>
        public void UnlistenEvent<T>(PreEventHandler<T> handler) => UnlistenEventImpl(TypeId<T>.Id, handler);

        /// <summary>
        /// 
        /// </summary>
        public void UnlistenEvent(int evtId, PostEventHandler<object> handler) => UnlistenEventImpl(evtId, handler);

        /// <summary>
        ///
        /// </summary>
        public void UnlistenEvent<T>(int evtId, PostEventHandler<T> handler) => UnlistenEventImpl(evtId, handler);

        /// <summary>
        ///
        /// </summary>
        public void UnlistenEvent<T>(int evtId, PreEventHandler<T> handler) => UnlistenEventImpl(evtId, handler);

        /// <summary>
        ///
        /// </summary>
        public virtual void UnlistenEventImpl(int evtId, in Delegate handler)
        {
            if (AllListens != null && AllListens.TryGetValue(evtId, out var list))
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    if (list.GetValueRef(i).Handler == handler)
                    {
                        if (_isDispatching > 0)
                        {
                            list.GetValueRef(i).Handler = null;
                            _dirtyRemoved = true;
                        }
                        else
                        {
                            list.RemoveAt(i);
                        }

                        return;
                    }
                }
            }

            if (PendingAddListens != null)
            {
                for (var i = 0; i < PendingAddListens.Count; i++)
                {
                    ref readonly var item = ref PendingAddListens.GetValueRef(i);
                    if (item.Item1 == evtId && item.Item2.Handler == handler)
                    {
                        PendingAddListens.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsListenEvent<T>(PostEventHandler<T> handler) => IsListenEventImpl(TypeId<T>.Id, handler);

        /// <summary>
        ///
        /// </summary>
        public bool IsListenEvent<T>(PreEventHandler<T> handler) => IsListenEventImpl(TypeId<T>.Id, handler);

        /// <summary>
        ///
        /// </summary>
        public bool IsListenEvent<T>(int evtId, PostEventHandler<T> handler) => IsListenEventImpl(evtId, handler);

        /// <summary>
        ///
        /// </summary>
        public bool IsListenEvent<T>(int evtId, PreEventHandler<T> handler) => IsListenEventImpl(evtId, handler);

        /// <summary>
        ///
        /// </summary>
        protected internal virtual bool IsListenEventImpl(Type evtType, Delegate handler) => IsListenEventImpl(evtType.GetHashCode(), handler);

        /// <summary>
        ///
        /// </summary>
        protected internal virtual bool IsListenEventImpl(int evtId, Delegate handler)
        {
            if (AllListens != null && AllListens.TryGetValue(evtId, out var list))
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (list.GetValueRef(i).Handler == handler)
                        return true;
                }
            }

            if (PendingAddListens != null)
            {
                for (var i = 0; i < PendingAddListens.Count; i++)
                {
                    ref readonly var item = ref PendingAddListens.GetValueRef(i);
                    if (item.Item1 == evtId && item.Item2.Handler == handler)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        public void ClearListenEvents()
        {
            if (_isDispatching > 0)
            {
                foreach (var list in AllListens)
                {
                    _dirtyRemoved = true;
                    for (var i = 0; i < list.Value.Count; i++)
                        list.Value.GetValueRef(i).Handler = null;
                }
            }
            else
            {
                AllListens?.Clear();
            }

            PendingAddListens?.Clear();
        }
    }
}