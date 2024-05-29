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
	[Obsolete("Use MapComponentCache for cached comp retrieval.")]
	public static class ComponentCache
	{
		public static SelfOrderingList<WorldComponent> worldComps = new SelfOrderingList<WorldComponent>();

		public static SelfOrderingList<GameComponent> gameComps = new SelfOrderingList<GameComponent>();

		/// <summary>
		/// Cache Retrieval for WorldComponents
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="world"></param>
		[Obsolete("Use vanilla retrieval instead.", true)]
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
			T comp = world.GetComponent<T>();
			worldComps.Add(comp);
			return comp;
		}

		/// <summary>
		/// Cache Retrieval for GameComponents
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="game"></param>
		[Obsolete("Use vanilla retrieval instead.", true)]
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
			T comp = game.GetComponent<T>();
			gameComps.Add(comp);
			return comp;
		}

		internal static void ClearCache()
		{
			worldComps.Clear();
			gameComps.Clear();
			MapComponentCache.ClearAll();
		}

		internal static void ClearMapComps(Map map)
		{
			MapComponentCache.ClearMap(map);
		}
	}
}
