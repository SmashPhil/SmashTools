﻿using System;
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
		/// Random item from those that fit conditions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="predicate"></param>
		public static T RandomOrDefault<T>(this IEnumerable<T> enumerable, Predicate<T> predicate = null)
		{
			if (enumerable.Where(item => predicate is null || predicate(item)).TryRandomElement(out T result))
			{
				return result;
			}
			return default(T);
		}

		/// <summary>
		/// Random item from those that fit conditions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="predicate"></param>
		/// <param name="fallback"></param>
		public static T RandomOrFallback<T>(this IEnumerable<T> enumerable, Predicate<T> predicate = null, T fallback = default(T))
		{
			if (enumerable.Where(item => predicate is null || predicate(item)).TryRandomElement(out T result))
			{
				return result;
			}
			return fallback;
		}

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

		public static bool NullOrEmpty<T>(this IEnumerable<T> enumerable)
		{
			if (enumerable == null)
			{
				return true;
			}
			return !enumerable.Any();
		}

		/// <summary>
		/// Uncached Count check with conditional predicate
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="predicate"></param>
		public static int CountWhere<T>(this IEnumerable<T> list, Predicate<T> predicate)
		{
			int count = 0;
			foreach (T item in list)
			{
				if (predicate(item)) { count++; }
			}
			return count;
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

		/// <summary>
		/// Prepend multiple objects to enumerable
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="items"></param>
		public static IEnumerable<T> AppendMany<T>(this IEnumerable<T> enumerable, T[] items)
		{
			for (int i = items.Count() - 1; i >= 0; i--)
			{
				enumerable = enumerable.Append(items[i]);
			}
			return enumerable;
		}

		/// <summary>
		/// Prepend multiple objects to enumerable
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="items"></param>
		public static IEnumerable<T> PrependMany<T>(this IEnumerable<T> enumerable, T[] items)
		{
			for (int i = items.Count() - 1; i >= 0; i--)
			{
				enumerable = enumerable.Prepend(items[i]);
			}
			return enumerable;
		}
	}
}
