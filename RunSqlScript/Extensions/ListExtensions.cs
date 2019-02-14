using System;
using System.Collections.Generic;

namespace RunSqlScript.Extensions
{
    public static class ListExtensions
    {
        public static void UpdateEachItem<T>(this IList<T> l, Func<T, T> getNewItem)
        {
            for (var i = 0; i < l.Count; i++)
            {
                l[i] = getNewItem(l[i]);
            }
        }
    }
}
