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
		/// Check if <paramref name="source"/> is entirely contained within <paramref name="target"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="target"></param>
		public static bool ContainsAllOfList<T>(this IEnumerable<T> source, IEnumerable<T> target)
		{
			if (source is null || target is null) return false;
			return source.Intersect(target).NotNullAndAny();
		}

		/// <summary>
		/// Creates a RotatingList&lt;<typeparamref name="T"/>&gt; from an IEnumerable&lt;<typeparamref name="T"/>&gt;
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sourceCollection"></param>
		public static RotatingList<T> ToRotatingList<T>(this IEnumerable<T> sourceCollection)
		{
			return new RotatingList<T>(sourceCollection);
		}

		/// <summary>
		/// Join list into readable string
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		public static string ToReadableString<T>(this IEnumerable<T> enumerable)
		{
			return string.Join(",", enumerable);
		}
	}
}
