using SmashTools.Xml;
using System.Collections.Generic;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationController : IAnimationFile
	{
		public const string FileExtension = ".ctrl";

		public List<AnimationState> states = new List<AnimationState>();

		public string FilePath { get; set; }

		public string FileName { get; set; }

		public string FileNameWithExtension => FileName + FileExtension;

		void IXmlExport.Export()
		{
			XmlExporter.WriteList(nameof(states), states);
		}

		public static implicit operator bool(AnimationController controller)
		{
			return controller != null;
		}
	}
}
