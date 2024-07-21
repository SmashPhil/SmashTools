using SmashTools.Xml;
using System.Collections.Generic;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationState : IXmlExport
	{
		public string name;
		public AnimationClip clip;
		public float speed = 1;
		public bool writeDefaults = true;

		public List<AnimationTransition> transitions;

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteElement(nameof(clip), clip.FilePath);
			XmlExporter.WriteElement(nameof(speed), speed.ToString());
			XmlExporter.WriteElement(nameof(writeDefaults), writeDefaults.ToString());

			XmlExporter.WriteList(nameof(transitions), transitions);
		}
	}
}
