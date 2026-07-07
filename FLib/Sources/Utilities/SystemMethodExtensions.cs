//==================={By Qcbf|qcbf@qq.com}===================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable CA1806

namespace FLib
{
    public static class SystemMethodExtensions
    {
        #region extends string class

        public static sbyte ToSByte(this string str) => sbyte.TryParse(str, out var result) ? result : (sbyte)str.ToFloat();
        public static sbyte ToSByte(this in ReadOnlySpan<char> str) => sbyte.TryParse(str, out var result) ? result : (sbyte)str.ToFloat();
        public static byte ToByte(this string str) => byte.TryParse(str, out var result) ? result : (byte)str.ToFloat();
        public static byte ToByte(this in ReadOnlySpan<char> str) => byte.TryParse(str, out var result) ? result : (byte)str.ToFloat();
        public static uint ToUInt(this string str) => uint.TryParse(str, out var result) ? result : (uint)str.ToDouble();
        public static uint ToUInt(this in ReadOnlySpan<char> str) => uint.TryParse(str, out var result) ? result : (uint)str.ToDouble();
        public static int ToInt(this string str) => int.TryParse(str, out var result) ? result : (int)str.ToDouble();
        public static int ToInt(this in ReadOnlySpan<char> str) => int.TryParse(str, out var result) ? result : (int)str.ToDouble();
        public static ushort ToUShort(this string str) => ToUShort(str.AsSpan());
        public static ushort ToUShort(this in ReadOnlySpan<char> str) => ushort.TryParse(str, out var result) ? result : (ushort)str.ToFloat();
        public static short ToShort(this string str) => short.TryParse(str, out var result) ? result : (short)str.ToFloat();
        public static short ToShort(this in ReadOnlySpan<char> str) => short.TryParse(str, out var result) ? result : (short)str.ToFloat();

        public static float ToFloat(this string str)
        {
            float.TryParse(str, out var rt);
            return rt;
        }

        public static float ToFloat(this in ReadOnlySpan<char> str)
        {
            float.TryParse(str, out var rt);
            return rt;
        }

        public static FNum ToFNum(this string str)
        {
            decimal.TryParse(str, out var rt);
            return (FNum)rt;
        }

        public static FNum ToFNum(this in ReadOnlySpan<char> str)
        {
            decimal.TryParse(str, out var rt);
            return (FNum)rt;
        }

        public static double ToDouble(this string str)
        {
            double.TryParse(str, out var rt);
            return rt;
        }

        public static double ToDouble(this in ReadOnlySpan<char> str)
        {
            double.TryParse(str, out var rt);
            return rt;
        }

        public static long ToLong(this string str) => long.TryParse(str, out var rt) ? rt : (long)str.ToDouble();
        public static long ToLong(this in ReadOnlySpan<char> str) => long.TryParse(str, out var rt) ? rt : (long)str.ToDouble();

        public static ulong ToULong(this string str)
        {
            ulong.TryParse(str, out var rt);
            return rt;
        }

        public static ulong ToULong(this in ReadOnlySpan<char> str)
        {
            ulong.TryParse(str, out var rt);
            return rt;
        }

        #endregion

        #region Type

        /// <summary>
        /// 获取当前类型默认值
        /// </summary>
        public static object DefaultValue(this Type t, bool strToEmpty = true)
        {
            if (t.IsValueType) return TypeAssistant.New(t);
            if (strToEmpty && t == typeof(string)) return string.Empty;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsStatic(this Type t)
        {
            return t.IsAbstract && t.IsSealed;
        }

        /// <summary> 是否为内置类型 </summary>
        public static bool IsBuiltInType(this Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type.Assembly == typeof(object).Assembly;
        }

        #endregion

        #region Task

        /// <summary>
        ///
        /// </summary>
        public static void When<T>(this Task<T> task, ROAction<T> action)
        {
            if (!task.IsCompleted)
            {
                task.ContinueWith(task1 =>
                {
                    if (!TaskFailureHandle(task1))
                        action(task.Result);
                }, TaskScheduler.Default);
            }
            else
            {
                if (!TaskFailureHandle(task))
                    action(task.Result);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static void Forget(this Task task)
        {
            if (task.IsCompleted)
                TaskFailureHandle(task);
            else
                task.ContinueWith(TaskFailureHandle, TaskScheduler.Default);
        }

        /// <summary>
        ///
        /// </summary>
        public static bool TaskFailureHandle(Task task)
        {
            if (task.Exception != null)
            {
                Log.Error?.Write(task.Exception);
                return true;
            }

            if (task.IsCanceled)
            {
                // Log.Info?.Write($"A task[{task.Id}] was canceled.");
                return true;
            }

            return false;
        }

        #endregion

        #region List

        /// <summary> 移除指定索引的元素，并将最后一个元素移动到指定索引位置 </summary>
        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            var theLastIndex = list.Count - 1;
            if (index < theLastIndex)
                list[index] = list[theLastIndex];
            list.RemoveAt(theLastIndex);
        }

        #endregion

        #region time

        public static string ToFormatString(this TimeSpan time, string ms = "ms", string s = "s", string m = "m", string h = "h", string d = "d")
        {
            var combiner = StringBufferCombiner.Create();
            if (time.TotalSeconds < 1)
                return combiner.Append(Math.Max(1, (long)time.TotalMilliseconds)).Append(ms).Result();
            if (time.TotalMinutes < 1)
                return combiner.Append((long)time.TotalSeconds).Append(s);
            if (time.TotalHours < 1)
                return combiner.Append((int)time.TotalMinutes).Append(m).Append(time.Seconds).Append(s).Result();
            return time.TotalDays < 1
                ? combiner.Append((int)time.TotalHours).Append(h).Append(time.Minutes).Append(m).Append(time.Seconds).Append(s).Result()
                : combiner.Append((int)time.TotalDays).Append(d).Append(time.Hours).Append(h).Append(time.Minutes).Append(m).Append(time.Seconds).Append(s).Result();
        }

        #endregion
    }
}