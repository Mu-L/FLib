// ==================== qcbf@qq.com | 2025-07-01 ====================

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public static class UIToolkitUtility
    {
        private static readonly Dictionary<MouseCursor, Cursor> Cursors = new();

        public static Cursor GetCursor(MouseCursor cursorType)
        {
            if (Cursors.TryGetValue(cursorType, out var cursor))
                return cursor;
            var boxingCursor = (object)cursor;
            typeof(Cursor).GetProperty("defaultCursorId", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(boxingCursor, (int)cursorType);
            cursor = (Cursor)boxingCursor;
            Cursors.Add(cursorType, cursor);
            return cursor;
        }
    }
}
