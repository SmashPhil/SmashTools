using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SmashTools
{
	public static class Ext_Comp
	{
		private static readonly FieldInfo thingCompComps = AccessTools.Field(typeof(ThingWithComps), "comps");

		/// <summary>
		/// Adds <paramref name="comp"/> to <paramref name="thingWithComps"/> and inits inner 'comps' list if empty.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="thingWithComps"></param>
		/// <param name="comp"></param>
		public static bool TryAddComp<T>(this ThingWithComps thingWithComps, T comp) where T : ThingComp
		{
			try
			{
				List<ThingComp> comps = thingWithComps.AllComps;
				if (comps.NullOrEmpty())
				{
					thingCompComps.SetValue(thingWithComps, new List<ThingComp>());
					comps = (List<ThingComp>)thingCompComps.GetValue(thingWithComps);
				}
				comps.Add(comp);
				return true;
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while trying to reflectively add <type>{comp.GetType()}</type> to {thingWithComps}.\nException={ex}");
			}
			return false;
		}
	}
}
