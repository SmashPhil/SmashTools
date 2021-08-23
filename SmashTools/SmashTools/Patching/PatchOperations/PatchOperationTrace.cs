using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using Verse;

namespace SmashTools
{
	public class PatchOperationTrace : PatchOperationPathed
	{
		protected override bool ApplyWorker(XmlDocument xml)
		{
			string[] nodes = Regex.Split(xpath, "/");
			string xpathChecking = nodes[0];
			for (int i = 1; i < nodes.Length - 1; i++)
			{
				if (xml.SelectSingleNode(xpathChecking) is null)
				{
					SmashLog.Error($"Failed at <text>{xpathChecking}</text>");
					return false;
				}
				xpathChecking += $"/{nodes[i]}";
			}
			SmashLog.Message($"<success>Successfully pathed to</success> {xpathChecking}");
			return true;
		}
	}
}
