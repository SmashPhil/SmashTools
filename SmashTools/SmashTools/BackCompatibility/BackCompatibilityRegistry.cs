using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace SmashTools
{
	public static class BackCompatibilityRegistry
	{
		private static readonly List<BackCompatibilityConverter> extraConverters = new List<BackCompatibilityConverter>();

		public static void RegisterBackCompatibility<T>(int major, int minor, string defName, bool defInjection = false, XmlNode node = null) where T : Def
		{
			throw new NotImplementedException();
		}
	}
}
