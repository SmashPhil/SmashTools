using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace SmashTools
{
	public static class Ext_RWTypes
	{
		public static void Deconstruct<Item1, Item2>(this Pair<Item1, Item2> pair, out Item1 item1, out Item2 item2)
		{
			item1 = pair.First;
			item2 = pair.Second;
		}

		public static IntVec2 RotatedBy(this IntVec2 orig, Rot4 rot)
		{
			return rot.AsInt switch
			{
				0 => orig,
				1 => new IntVec2(orig.z, -orig.x),
				2 => new IntVec2(-orig.x, -orig.z),
				3 => new IntVec2(-orig.z, orig.x),
				_ => orig,
			};
		}
	}
}
