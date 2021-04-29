using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public static class Ext_Mods
	{
		public static bool HasActiveModWithPackageId(string packageId)
		{
			return ModLister.GetActiveModWithIdentifier(packageId) != null;
		}
	}
}
