using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Animations
{
	public sealed class AnimationClip
	{
		public const string FileExtension = "rwa";

		public int frameCount;
		
		public List<AnimationProperty> properties;

		public string FileName { get; internal set; }

		public string FilePath { get; internal set; }

		public void WriteData()
		{
			XmlExporter.WriteElement(nameof(frameCount), frameCount.ToString());
			if (properties != null)
			{
				XmlExporter.OpenNode(nameof(properties));
				{
					foreach (AnimationProperty property in properties)
					{
						XmlExporter.OpenNode("li");
						{
							property.WriteData();
						}
						XmlExporter.CloseNode();
					}
				}
				XmlExporter.CloseNode();
			}
		}
	}
}
