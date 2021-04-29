using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace SmashTools
{
	public class PatchOperationFindModId : PatchOperation
	{

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 //Never assigned to
		private List<string> mods;
		private PatchOperation match;
		private PatchOperation nomatch;
#pragma warning restore CS0649 //Never assigned to
#pragma warning restore IDE0044 // Add readonly modifier

		protected override bool ApplyWorker(XmlDocument xml)
		{
			for (int i = 0; i < mods.Count; i++)
			{
				if (Ext_Mods.HasActiveModWithPackageId(mods[i]))
				{
					return match?.Apply(xml) ?? true;
				}
			}
			return nomatch?.Apply(xml) ?? true;
		}

		public override string ToString()
		{
			return string.Format("{0}({1})", base.ToString(), mods.ToCommaList(false));
		}
	}
}
