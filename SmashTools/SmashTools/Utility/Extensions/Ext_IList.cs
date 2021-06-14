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
		/// Pop random value from List
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <returns></returns>
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
		/// <returns></returns>
		public static T Next<T>(this IList<T> list, T current)
		{
			int indexOf = list.IndexOf(current) + 1;
			if (indexOf >= list.Count)
				indexOf = 0;
			return list[indexOf];
		}

		/// <summary>
		/// Split list on item T and shift to front
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <returns></returns>
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
	}
}
