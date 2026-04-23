// ==================={By Qcbf|qcbf@qq.com|12/7/2022 9:15:05 PM}===================

using System;
using System.Collections.Generic;
using FLib;

namespace FLib
{
    public delegate void RWAction<T>(ref T v);

    public delegate void RWAction<T1, T2>(ref T1 v1, ref T2 v2);

    public delegate void ROAction<T>(in T v);

    public delegate void ROAction<T1, T2>(in T1 v1, in T2 v2);
}