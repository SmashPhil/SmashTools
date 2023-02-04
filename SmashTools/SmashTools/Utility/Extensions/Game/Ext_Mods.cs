using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public static class Ext_Mods
	{
		public static bool HasActiveMod(string packageId)
		{
			return ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null;
		}
	}
}
