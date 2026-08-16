//==================={By Qcbf|qcbf@qq.com|12/10/2022 4:30:36 PM}===================

using System;
using System.Globalization;

namespace FLib
{
    public class StringFLibFormatter : IFormatProvider, ICustomFormatter
    {
        public static StringFLibFormatter Main = new();

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
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }
}
