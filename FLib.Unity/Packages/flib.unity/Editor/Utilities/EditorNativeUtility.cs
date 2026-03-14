//==================={By Qcbf|qcbf@qq.com|12/28/2021 10:56:55 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FLib.Unity.Editor
{
    public static class EditorNativeUtility
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(byte[] b1, byte[] b2, long count);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(IntPtr b1, IntPtr b2, long count);



    }
}
