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
        public static Dictionary<Map, SelfOrderingList<MapComponent>> mapDict = new Dictionary<Map, SelfOrderingList<MapComponent>>();

        public static SelfOrderingList<WorldComponent> worldComps = new SelfOrderingList<WorldComponent>();

        public static SelfOrderingList<GameComponent> gameComps = new SelfOrderingList<GameComponent>();

        /// <summary>
        /// Cache Retrieval for MapComponents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="map"></param>
        /// <returns></returns>
        public static T GetCachedMapComponent<T>(this Map map) where T : MapComponent
        {
            if(mapDict.TryGetValue(map, out var mapComps))
            {
                for (int i = 0; i < mapComps.Count; i++)
			    {
				    if (mapComps[i] is T t)
				    {
                        mapComps.CountIndex(i);
					    return t;
				    }
			    }
                MapComponent component = map.GetComponent<T>();
                if (component != null)
                {
                    mapComps.Add(component);
                }
                return (T)component;
            }
            mapDict.Add(map, new SelfOrderingList<MapComponent>(map.components));
            T comp = map.GetComponent<T>();
            return comp;
        }

        /// <summary>
        /// Cache Retrieval for WorldComponents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="world"></param>
        /// <returns></returns>
        public static T GetCachedWorldComponent<T>(this World _) where T : WorldComponent
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
        /// <returns></returns>
        public static T GetCachedGameComponent<T>(this Game _) where T : GameComponent
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
            foreach (WorldComponent component in __instance.components)
            {
                worldComps.Add(component);
            }
        }

        internal static void ConstructGameComponents(Game __instance)
        {
            foreach (GameComponent component in __instance.components)
            {
                gameComps.Add(component);
            }
        }
    }
}
