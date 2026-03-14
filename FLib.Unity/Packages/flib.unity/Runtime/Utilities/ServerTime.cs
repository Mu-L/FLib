//==================={By Qcbf|qcbf@qq.com|9/24/2022 9:36:16 PM}===================

using System;
using System.Collections.Generic;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public static class ServerTime
    {
        public static float Offset;
        public static long LastServerTime;

        public static DateTime Now => TimeHelper.Default.TimestampMSToDate(NowTimestampMS);
        public static long NowTimestamp => LastServerTime / 1000 + (long)(Time.realtimeSinceStartup - Offset);
        public static long NowTimestampMS => LastServerTime + (long)((Time.realtimeSinceStartup - Offset) * 1000);

        public static void SetServerTime(long t)
        {
            LastServerTime = t;
            Offset = Time.realtimeSinceStartup;
        }
    }
}
