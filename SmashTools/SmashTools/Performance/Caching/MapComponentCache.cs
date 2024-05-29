using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;
using RimWorld;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class MapComponentCache
	{
		private static readonly List<Type> priorityComponentTypes = new List<Type>();

		static MapComponentCache()
		{
			priorityComponentTypes = GenTypes.AllSubclassesNonAbstract(typeof(MapComponent)).ToList();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetCachedMapComponent<T>(this Map map) where T : MapComponent
		{
			return MapComponentCache<T>.GetComponent(map);
		}

		internal static void ClearMap(Map map)
		{
			foreach (Type type in priorityComponentTypes)
			{
				GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type, nameof(MapComponentCache<MapComponent>.ClearMap), map);
			}
		}

		internal static void ClearAll()
		{
			foreach (Type type in priorityComponentTypes)
			{
				GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type, nameof(MapComponentCache<MapComponent>.ClearAll));
			}
		}
	}

	public static class MapComponentCache<T> where T : MapComponent
	{
		public static T[] mapComps = new T[sbyte.MaxValue];

		public static T GetComponent(Map map)
		{
			T component = mapComps[map.Index];
			if (component == null)
			{
				component = map.GetComponent<T>();
				mapComps[map.Index] = component;
			}
			return component;
		}

		public static void ClearMap(Map map)
		{
			mapComps[map.Index] = null; //Free up cached component so it can be fetched when index is reused
		}

		public static void ClearAll()
		{
			mapComps = new T[sbyte.MaxValue];
		}
	}
}
