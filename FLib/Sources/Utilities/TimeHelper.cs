// =================================================={By Qcbf|qcbf@qq.com|2024-10-22}==================================================

using System;
using System.Text;

namespace FLib
{
    public readonly struct TimeHelper
    {
        public static readonly TimeHelper Default = new(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        public static uint Timestamp => Default.GetTimestamp();
        public static long TimestampMs => Default.GetTimestampMs();


        public readonly DateTime BaseDate;

        public TimeHelper(DateTime baseDate)
        {
            BaseDate = baseDate;
        }

        public uint GetTimestamp() => DateToTimestamp(DateTime.UtcNow);
        public long GetTimestampMs() => DateToTimestampMs(DateTime.UtcNow);

        /// <summary>
        /// 时间戳转换c#时间
        /// </summary>
        public DateTime TimestampToDate(long timestamp) => BaseDate.AddSeconds(timestamp).ToLocalTime();

        /// <summary>
        /// 时间戳转换c#时间
        /// </summary>
        public DateTime TimestampMSToDate(long timestamp) => BaseDate.AddMilliseconds(timestamp).ToLocalTime();

        /// <summary>
        /// c#时间转换为时间戳
        /// </summary>
        public uint DateToTimestamp(DateTime date)
        {
            if (date.Kind != DateTimeKind.Utc)
                date = date.ToUniversalTime();
            return (uint)((date.Ticks - Default.BaseDate.Ticks) / TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// c#时间转换为时间戳(毫秒)
        /// </summary>
        public long DateToTimestampMs(DateTime date)
        {
            if (date.Kind != DateTimeKind.Utc)
                date = date.ToUniversalTime();
            return (date.Ticks - Default.BaseDate.Ticks) / TimeSpan.TicksPerMillisecond;
        }

        /// <summary> 格式化时间 </summary>
        public static string FormatSeconds(int seconds, string dayText = "d ", string hourText = "h ", string minuteText = "m ", string secondText = "s")
        {
            return StringFLibUtility.ReleaseStrBufAndResult(FormatSeconds(StringFLibUtility.GetStrBuf(), seconds, dayText, hourText, minuteText, secondText));
        }

        /// <summary> 格式化时间 </summary>
        public static StringBuilder FormatSeconds(StringBuilder strbuf, int seconds, string dayText = "d ", string hourText = "h ", string minuteText = "m ", string secondText = "s")
        {
            var days = seconds / 86400;
            seconds -= days * 86400;
            var hours = seconds / 3600;
            seconds -= hours * 3600;
            var minutes = seconds / 60;
            seconds -= minutes * 60;
            var hasHour = days > 0 || hours > 0;
            var hasMinute = hasHour || minutes > 0;

            if (days > 0)
                strbuf.Append(days).Append(dayText);
            if (hasHour)
                strbuf.Append(hours).Append(hourText);
            if (hasMinute)
                strbuf.Append(minutes).Append(minuteText);
            strbuf.Append(seconds).Append(secondText);
            return strbuf;
        }
    }
}
