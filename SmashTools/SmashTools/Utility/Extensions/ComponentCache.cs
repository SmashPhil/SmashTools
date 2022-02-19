using System;
using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace SmashTools
{
	/// <summary>
	/// SelfOrderingList implementations for caching of components for Map, World, and Game objects
	/// </summary>
	/// <remarks>Cuts performance impact for component retrieval to less than half the original average</remarks>
	[StaticConstructorOnStartup]
	public static class ComponentCache
	{
		public static SelfOrderingList<MapComponent>[] mapComps = new SelfOrderingList<MapComponent>[sbyte.MaxValue].Populate(() => new SelfOrderingList<MapComponent>());

		public static SelfOrderingList<WorldComponent> worldComps = new SelfOrderingList<WorldComponent>();

		public static SelfOrderingList<GameComponent> gameComps = new SelfOrderingList<GameComponent>();

		/// <summary>
		/// Cache Retrieval for MapComponents
		/// </summary>
		/// <remarks>
		/// DO NOT USE if you do not know if the MapComponent exists
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="map"></param>
		public static T GetCachedMapComponent<T>(this Map map) where T : MapComponent
		{
			var comps = mapComps[map.Index];
			if (!comps.NullOrEmpty())
			{
				for (int i = 0; i < comps.Count; i++)
				{
					if (comps[i] is T t)
					{
						comps.CountIndex(i);
						return t;
					}
				}
				MapComponent component = map.GetComponent<T>();
				if (component != null)
				{
					comps.Add(component);
				}
				return (T)component;
			}
			mapComps[map.Index] = new SelfOrderingList<MapComponent>(map.components);
			T comp = map.GetComponent<T>();
			return comp;
		}

		/// <summary>
		/// Cache Retrieval for WorldComponents
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="world"></param>
		public static T GetCachedWorldComponent<T>(this World world) where T : WorldComponent
		{
			for (int i = 0; i < worldComps.Count; i++)
			{
				if (worldComps[i] is T t)
				{
					worldComps.CountIndex(i);
					return t;
				}
			}
			return default;
		}

		/// <summary>
		/// Cache Retrieval for GameComponents
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="game"></param>
		public static T GetCachedGameComponent<T>(this Game game) where T : GameComponent
		{
			for (int i = 0; i < gameComps.Count; i++)
			{
				if (gameComps[i] is T t)
				{
					gameComps.CountIndex(i);
					return t;
				}
			}
			return default;
		}


		/* Initialization of sorted lists */

		internal static void ConstructWorldComponents(World __instance)
		{
			worldComps = new SelfOrderingList<WorldComponent>(__instance.components);
		}

		internal static void ConstructGameComponents(Game __instance)
		{
			gameComps = new SelfOrderingList<GameComponent>(__instance.components);
		}

		internal static void ClearAllMapComps()
		{
			mapComps = new SelfOrderingList<MapComponent>[sbyte.MaxValue].Populate(() => new SelfOrderingList<MapComponent>());
		}

		internal static void ClearMapComps(Map map)
		{
			ClearMapComps(map.Index);
		}

		internal static void ClearMapComps(int index)
		{
			mapComps[index].Clear();
		}

		internal static void MapGenerated(IEnumerable<GenStepWithParams> genStepDefs, Map map, int seed)
		{
			ClearMapComps(map.Index);
		}

		internal static void RegisterMapComps(Map map)
		{
			mapComps[map.Index].AddRange(map.components);
		}
	}
}
