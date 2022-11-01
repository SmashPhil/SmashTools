using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace SmashTools
{
	public class PatchOperationAddOrReplace : PatchOperationPathed
	{
#pragma warning disable IDE0044 // Add readonly modifier
		private XmlContainer value;
		private Order order = Order.Append;
#pragma warning restore IDE0044 // Add readonly modifier

		protected override bool ApplyWorker(XmlDocument xml)
		{
			XmlNode node = value.node;
			bool result = false;
			foreach (XmlNode xmlNode in xml.SelectNodes(xpath))
			{
				result = true;
				switch (order)
				{
					case Order.Append:
						{
							for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
							{
								xmlNode.AppendChild(xmlNode.OwnerDocument.ImportNode(node.ChildNodes[i], true));
							}
						}
						break;
					case Order.Prepend:
						{
							for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
							{
								xmlNode.PrependChild(xmlNode.OwnerDocument.ImportNode(node.ChildNodes[i], true));
							}
						}
						break;
				}
			}
			return result;
		}

		private enum Order
		{
			Append,
			Prepend
		}
	}
}
