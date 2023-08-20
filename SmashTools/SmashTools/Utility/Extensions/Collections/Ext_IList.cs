using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	public static class Ext_IList
	{
		/// <summary>
		/// Populate array with <paramref name="value"/> up to <paramref name="count"/> times.
		/// </summary>
		/// <remarks>Overwrites existing values, as list may or may not be pre-filled and of a smaller size than <paramref name="count"/></remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="value"></param>
		public static void Populate<T>(this IList<T> list, T value, int count)
		{
			list.Clear();
			for (int i = 0; i < count; ++i)
			{
				list.Add(value);
			}
		}

		/// <summary>
		/// Pop random value from List
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		public static T PopRandom<T>(this IList<T> list)
		{
			if (!list.NotNullAndAny())
			{
				return default;
			}
			Rand.PushState();
			int index = Rand.Range(0, list.Count);
			T item = list.PopAt(index);
			Rand.PopState();
			return item;
		}

		/// <summary>
		/// Pop element at <paramref name="index"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		public static T PopAt<T>(this IList<T> list, int index)
		{
			T r = list[index];
			list.RemoveAt(index);
			return r;
		}

		/// <summary>
		/// Grabs the next item in the list, given a reference item as the current position
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="current"></param>
		public static T Next<T>(this IList<T> list, T current)
		{
			int indexOf = list.IndexOf(current) + 1;
			if (indexOf >= list.Count)
			{
				indexOf = 0;
			}
			return list[indexOf];
		}

		/// <summary>
		/// Swap 2 elements in list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index1"></param>
		/// <param name="index2"></param>
		public static IList<T> Swap<T>(this IList<T> list, int index1, int index2)
		{
			T tmpItem = list[index1];
			list[index1] = list[index2];
			list[index2] = tmpItem;
			return list;
		}

		/// <summary>
		/// Split list on item T and shift to front
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		public static List<T> ReorderOn<T>(this List<T> list, T item)
		{
			int index = list.IndexOf(item);
			if (index < 0)
			{
				Log.Error($"Unable to ReorderOn list, item does not exist.");
				return list;
			}
			var frontList = list.GetRange(0, index);
			var backList = list.GetRange(index + 1, list.Count - 1);
			var newList = new List<T>(backList);
			newList.AddRange(frontList);
			return newList;
		}

		/// <summary>
		/// Check if index is within bounds of list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		public static bool OutOfBounds<T>(this IList<T> list, int index)
		{
			if (list.NullOrEmpty())
			{
				return true;
			}
			return index < 0 || index >= list.Count;
		}
	}
}
