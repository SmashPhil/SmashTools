using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
    public static class Ext_IEnumerable
    {
        /// <summary>
        /// .Any extension method but takes into account null collections do not contain the object. 
        /// Does not throw error on null collections which is more applicable to this project.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool NotNullAndAny<T>(this IEnumerable<T> enumerable, Predicate<T> predicate = null)
        {
            return enumerable != null && (predicate is null ? enumerable.Any() : enumerable.Any(e => predicate(e)));
        }

        /// <summary>
        /// Performs the specified action on each element of <c>IEnumerable <typeparamref name="T"/></c>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach(T item in enumerable)
            {
                action(item);
            }
        }

        /// <summary>
        /// Check if one List is entirely contained within another List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceList"></param>
        /// <param name="searchingList"></param>
        public static bool ContainsAllOfList<T>(this IEnumerable<T> sourceList, IEnumerable<T> searchingList)
        {
            if (sourceList is null || searchingList is null) return false;
            return sourceList.Intersect(searchingList).NotNullAndAny();
        }
    }
}
