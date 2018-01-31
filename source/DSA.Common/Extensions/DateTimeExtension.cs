using System;

namespace DSA.Common.Extensions
{
    /// <summary>
    /// DateTime Extensions
    /// </summary>
    public static class DateTimeExtension
    {
        public static string ToShortDateString(this DateTime date)
        {
            return date.ToString(System.Globalization.DateTimeFormatInfo.CurrentInfo.ShortDatePattern); 
        }

        public static string ToLongDateString(this DateTime date)
        {
            return date.ToString(System.Globalization.DateTimeFormatInfo.CurrentInfo.LongDatePattern);
        }
    }
}
