//==================={By Qcbf|qcbf@qq.com|12/10/2022 4:30:36 PM}===================

using System;
using System.Globalization;

namespace FLib
{
    public class StringFLibFormatter : IFormatProvider, ICustomFormatter
    {
        public static StringFLibFormatter Default = new();

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                return string.Format(formatProvider, format!, arg);
            return format[0] switch
            {
                '%' => (Convert.ToDouble(arg) * 0.01).ToString("0.##") + "%",
                '‰' => (Convert.ToDouble(arg) * 0.001).ToString("0.##") + "%",
                '‱' => (Convert.ToDouble(arg) * 0.0001).ToString("0.##") + "%",
                '+' => (Convert.ToDouble(arg) + format.AsSpan(1).ToDouble()).ToString("0.##"),
                '-' => (Convert.ToDouble(arg) - format.AsSpan(1).ToDouble()).ToString("0.##"),
                '*' => (Convert.ToDouble(arg) * format.AsSpan(1).ToDouble()).ToString("0.##"),
                '/' => (Convert.ToDouble(arg) / format.AsSpan(1).ToDouble()).ToString("0.##"),
                _ => format == "abs" ? Math.Abs(Convert.ToDouble(arg)).ToString(CultureInfo.InvariantCulture) : string.Format(formatProvider, format, arg)
            };
        }

        public object GetFormat(Type formatType)
            => formatType == typeof(ICustomFormatter) ? this : null;

        public static string Format(string format)
            => string.Format(Default, format);

        public static string Format(string format, object arg1)
            => string.Format(Default, format, arg1);

        public static string Format(string format, object arg1, object arg2)
            => string.Format(Default, format, arg1, arg2);

        public static string Format(string format, object arg1, object arg2, object arg3)
            => string.Format(Default, format, arg1, arg2, arg3);

        public static string Format(string format, params object[] args)
            => string.Format(Default, format, args);
    }
}