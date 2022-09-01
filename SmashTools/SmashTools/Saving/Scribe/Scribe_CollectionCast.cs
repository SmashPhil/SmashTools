using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SmashTools
{
	public static class Scribe_CollectionCast
	{
		public static void Look<T>(ref List<T> list, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
		{
			Look(ref list, false, label, lookMode, ctorArgs);
		}

		public static void Look<T>(ref List<T> list, bool saveDestroyedThings, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
		{
			if (lookMode == LookMode.Undefined && !Scribe_Universal.TryResolveLookMode(typeof(T), out lookMode, false, false))
			{
				Log.Error("LookList call with a list of " + typeof(T) + " must have lookMode set explicitly.");
				return;
			}
			if ((lookMode != LookMode.Reference && lookMode != LookMode.Deep) || Scribe.mode != LoadSaveMode.Saving)
			{
				Scribe_Collections.Look(ref list, saveDestroyedThings, label, lookMode, ctorArgs);
				return;
			}
			if (Scribe.EnterNode(label))
			{
				try
				{
					if (list == null)
					{
						Scribe.saver.WriteAttribute("IsNull", "True");
						return;
					}
					for (int i = 0; i < list.Count; i++)
					{
						T item = list[i];
						if (lookMode == LookMode.Deep)
						{
							Scribe_Deep.Look(ref item, saveDestroyedThings, "li", ctorArgs);
							Scribe.saver.WriteAttribute("Class", typeof(T).ToString());
						}
						else if (lookMode == LookMode.Reference)
						{
							ILoadReferenceable loadReferenceable = (ILoadReferenceable)(object)item;
							Scribe_References.Look(ref loadReferenceable, "li", saveDestroyedThings);
							Scribe.saver.WriteAttribute("Class", typeof(T).ToString());
						}
					}
				}
				finally
				{
					Scribe.ExitNode();
				}
			}
		}

		public static void Look<K, V>(ref Dictionary<K, V> dict, string label, LookMode keyLookMode = LookMode.Undefined, LookMode valueLookMode = LookMode.Undefined)
		{
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				bool flag = keyLookMode == LookMode.Reference;
				bool flag2 = valueLookMode == LookMode.Reference;
				if (flag != flag2)
				{
					Log.Error("You need to provide working lists for the keys and values in order to be able to load such dictionary. label=" + label);
				}
			}
			List<K> list = null;
			List<V> list2 = null;
			Look(ref dict, label, keyLookMode, valueLookMode, ref list, ref list2);
		}

		public static void Look<K, V>(ref Dictionary<K, V> dict, string label, LookMode keyLookMode, LookMode valueLookMode, ref List<K> keysWorkingList, ref List<V> valuesWorkingList)
		{
			if (Scribe.mode != LoadSaveMode.Saving || (keyLookMode != LookMode.Deep && keyLookMode != LookMode.Reference && valueLookMode != LookMode.Deep && valueLookMode != LookMode.Reference))
			{
				Scribe_Collections.Look(ref dict, label, keyLookMode, valueLookMode, ref keysWorkingList, ref valuesWorkingList);
				return;
			}
			if (Scribe.EnterNode(label))
			{
				try
				{
					if (dict == null)
					{
						Scribe.saver.WriteAttribute("IsNull", "True");
						return;
					}
					keysWorkingList = new List<K>();
					valuesWorkingList = new List<V>();
					if (dict != null)
					{
						foreach (KeyValuePair<K, V> keyValuePair in dict)
						{
							keysWorkingList.Add(keyValuePair.Key);
							valuesWorkingList.Add(keyValuePair.Value);
						}
					}
					Look(ref keysWorkingList, "keys", keyLookMode);
					Look(ref valuesWorkingList, "values", valueLookMode);
				}
				finally
				{
					Scribe.ExitNode();
				}
			}
		}
	}
}
