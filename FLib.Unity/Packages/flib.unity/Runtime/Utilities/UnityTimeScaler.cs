// ==================== qcbf@qq.com | 2025-09-18 ====================

using System.Collections.Generic;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public static class UnityTimeScaler
    {
        public static Dictionary<string, Entry> Entries = new();
        public static Entry Current = Entry.Min;

        public struct Entry
        {
            public static readonly Entry Min = new() { Scale = 1, Priority = int.MinValue };
            public string Key;
            public int Priority;
            public float Scale;
            public override string ToString() => $"[{Priority}]{Key}:{Scale}";
            public void Active() => Time.timeScale = Scale;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Set(string key, float scale, int priority = 0) => Set(new Entry() { Key = key, Priority = priority, Scale = scale });

        /// <summary>
        /// 
        /// </summary>
        public static void Set(in Entry entry)
        {
            Log.Verbose?.Write(entry.ToString(), "TimeScale", "Set");
            Entries[entry.Key] = entry;
            if (entry.Priority >= Current.Priority)
                (Current = entry).Active();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Unset(string key)
        {
            Log.Verbose?.Write(key, "TimeScale", "Unset");
            if (Entries.Remove(key, out var curEntry) && (Current.Priority < curEntry.Priority || Current.Key == key))
            {
                var bestEntry = Entry.Min;
                foreach (var entry in Entries)
                {
                    if (entry.Value.Priority >= bestEntry.Priority)
                        bestEntry = entry.Value;
                }
                (Current = bestEntry).Active();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void UpdateTimeScale()
        {
            var bestEntry = new Entry() { Priority = int.MinValue, Scale = 1 };
            foreach (var entry in Entries)
            {
                if (entry.Value.Priority >= bestEntry.Priority)
                    bestEntry = entry.Value;
            }
            Current = bestEntry;
            Time.timeScale = bestEntry.Scale;
        }
    }
}
