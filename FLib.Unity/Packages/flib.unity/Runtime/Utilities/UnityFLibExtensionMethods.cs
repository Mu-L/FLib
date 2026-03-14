//==================={By Qcbf|qcbf@qq.com|11/3/2021 11:53:50 AM}===================
// ReSharper disable RedundantCast

using FLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace FLib.Unity
{
    public static class UnityFLibExtensionMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectTransform AsRectTf(this Transform transform) => transform as RectTransform;

        #region as vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVec2XZ(this in Vector3 v) => new(v.x, v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector2 AsFVec2XY(this in Vector3 v) => new((FNum)v.x, (FNum)v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector2 AsFVec2XZ(this in Vector3 v) => new((FNum)v.x, (FNum)v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVec3XY(this in Vector2 v, float z = 0) => new(v.x, v.y, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVec3XZ(this in Vector2 v, float y = 0) => new(v.x, y, v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVec3XY(this in FVector2 v, float z = 0) => new(v.X, v.Y, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVec3XZ(this in FVector2 v, float y = 0) => new(v.X, y, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector2 AsFVec2(this in Vector2 v) => new((FNum)v.x, (FNum)v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVec(this in FVector3 v) => new(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVec(this in FVector2 v) => new(v.X, v.Y);
        #endregion

        #region set xyz
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SetX(this Vector3 v, float t) => new(t, v.y, v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SetY(this Vector3 v, float t) => new(v.x, t, v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SetZ(this Vector3 v, float t) => new(v.x, v.y, t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SetXY(this Vector3 v, float x, float y) => new(x, y, v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SetXZ(this Vector3 v, float x, float z) => new(x, v.y, z);
        #endregion

        #region color
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetColorAlpha(this Graphic c, float v)
        {
            var col = c.color;
            col.a = v;
            c.color = col;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color SetColorAlpha(this Color c, float v)
        {
            c.a = v;
            return c;
        }
        #endregion

        #region linq extensions
        /// <summary>
        /// 
        /// </summary>
        public static PooledList<TSource> ToPooledList<TSource>(this IEnumerable<TSource> source)
        {
            Log.AssertNotNull(source);
            var list = new PooledList<TSource>();
            if (source is ICollection<TSource> collection)
            {
                list.Allocate(collection.Count);
                collection.CopyTo(list.Buffer, 0);
                return list;
            }
            list.Allocate(16);
            foreach (var item in source)
                list.Add(item);
            return list;
        }
        #endregion
    }
}
