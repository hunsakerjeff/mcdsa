using System;

namespace DSA.Common.Utils
{
    public static class MathUtil
    {
        //http://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
        public static string BytesToString(UInt64 byteCount)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            int order = 0;
            while (byteCount >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                byteCount = byteCount / 1024;
            }
            if (order < sizes.Length - 1)
            {
                return string.Format("{0:0.#}{1}", byteCount, sizes[order]);
            }
            return "?";
        }

        public static string BytesToString(decimal byteCount)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            int order = 0;
            while (byteCount >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                byteCount = byteCount / 1024;
            }
            if (order < sizes.Length - 1)
            {
                return string.Format("{0:0.#}{1}", byteCount, sizes[order]);
            }
            return "?";
        }
    }
}