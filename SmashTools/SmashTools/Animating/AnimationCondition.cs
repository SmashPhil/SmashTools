using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmashTools.Xml;

namespace SmashTools.Animations
{
	public struct AnimationCondition : IXmlExport
	{
		public string parameter;
		public ComparisonType comparison;

		public void Export()
		{
			XmlExporter.WriteElement(nameof(parameter), parameter);
			XmlExporter.WriteObject(nameof(comparison), comparison);
		}
	}
}
