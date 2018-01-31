using System.Collections.Generic;

namespace DSA.Common.Extensions
{
    /// <summary>
    /// List Extensions
    /// </summary>
    public static class ListExtension
    {
        public static List<T> AppendList<T>(this List<T> list, IEnumerable<T> toAppend)
        {
            list.AddRange(toAppend);
            return list;
        }
    }

}
