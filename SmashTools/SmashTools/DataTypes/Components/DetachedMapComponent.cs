using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	/// <summary>
	/// Can be retrieved with map component indexing, but doesn't piggy back off <see cref="MapComponent"/>, and is not cached by Map.
	/// </summary>
	/// <remarks>This is useful for behavior that only needs to exist alongside a map reference. ie does not need to tick, save, init on load, etc.</remarks>
	public abstract class DetachedMapComponent
	{
		protected readonly Map map;

		internal static Dictionary<Map, SelfOrderingList<DetachedMapComponent>> mapComps = new Dictionary<Map, SelfOrderingList<DetachedMapComponent>>();

		private static HashSet<Type> usedComponentTypes = new HashSet<Type>();

		public DetachedMapComponent(Map map)
		{
			this.map = map;
			mapComps[map].Add(this);
		}

		protected virtual void PreMapRemoval()
		{
		}

		internal static void InstantiateAllMapComponents(Map __instance)
		{
			mapComps[__instance] = new SelfOrderingList<DetachedMapComponent>();
			usedComponentTypes.Clear();
			foreach (Type type in typeof(DetachedMapComponent).AllSubclassesNonAbstract())
			{
				if (!usedComponentTypes.Contains(type))
				{
					try
					{
						DetachedMapComponent mapComponent = (DetachedMapComponent)Activator.CreateInstance(type, new object[] { __instance });
						mapComps[__instance].Add(mapComponent);
						usedComponentTypes.Add(type);
					}
					catch (Exception ex)
					{
						Log.Error($"Could not instantiate DetachedMapComponent of type {type}. Exception={ex}");
					}
				}
			}
			usedComponentTypes.Clear();
		}

		internal static void ClearComponentsFromCache(Map map)
		{
			foreach (DetachedMapComponent mapComponent in mapComps[map])
			{
				mapComponent.PreMapRemoval();
			}
			mapComps.Remove(map);
		}
	}
}
