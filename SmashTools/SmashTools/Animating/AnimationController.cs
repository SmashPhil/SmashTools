using SmashTools.Xml;
using System.Collections.Generic;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationController : IXmlExport
	{
		public List<AnimationState> states;

		void IXmlExport.Export()
		{
			XmlExporter.WriteList(nameof(states), states);
		}
	}
}
