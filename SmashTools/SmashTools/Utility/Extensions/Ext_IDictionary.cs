using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	public static class Ext_IDictionary
	{
		/// <summary>
		/// Grab random value from dictionary
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="dictionary"></param>
		public static KeyValuePair<K, V> RandomKVPFromDictionary<K, V>(this IDictionary<K, V> dictionary)
		{
			Rand.PushState();
			KeyValuePair<K, V> result = dictionary.ElementAt(Rand.Range(0, dictionary.Count));
			Rand.PopState();
			return result;
		}

		/// <summary>
		/// Try to add key value pair to dictionary
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns>False if <paramref name="key"/> already exists in <paramref name="dictionary"/></returns>
		public static bool TryAdd<K, V>(this IDictionary<K, V> dictionary, K key, V value)
		{
			if (dictionary.ContainsKey(key))
			{
				return false;
			}
			dictionary.Add(key, value);
			return true;
		}

		/// <summary>
		/// Add <paramref name="value"/> for <paramref name="key"/> into <paramref name="dictionary"/> or replace old <paramref name="value"/>
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void AddOrReplace<K, V>(this IDictionary<K, V> dictionary, K key, V value)
		{
			if (!dictionary.TryAdd(key, value))
			{
				dictionary[key] = value;
			}
		}

		/// <summary>
		/// Add <paramref name="value"/> as new entry into <paramref name="dictionary"/> or insert into existing list
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void AddOrInsert<K, V>(this IDictionary<K,List<V>> dictionary, K key, V value)
		{
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, new List<V>());
			}
			dictionary[key].Add(value);
		}

		/// <summary>
		/// <seealso cref="AddOrInsert{K, V}(IDictionary{K, List{V}}, K, V)"/>
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void AddOrInsert<K, V>(this IDictionary<K, HashSet<V>> dictionary, K key, V value)
		{
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, new HashSet<V>());
			}
			dictionary[key].Add(value);
		}
	}
}
