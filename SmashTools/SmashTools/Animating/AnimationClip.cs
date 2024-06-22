using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	public sealed class AnimationClip
	{
		public const string FileExtension = "rwa";

		public int frameCount;

		public string Name { get; internal set; }

		public string FilePath { get; internal set; }

		public void Export()
		{
			XmlExporter.WriteElement(nameof(frameCount), frameCount.ToString());
		}
	}
}
