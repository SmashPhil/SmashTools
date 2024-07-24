using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Verse;
using static SmashTools.Animations.AnimationState;

namespace SmashTools.Animations
{
	public class AnimationController : IAnimationFile
	{
		public const string FileExtension = ".ctrl";

		public List<AnimationLayer> layers = new List<AnimationLayer>();

		private AnimationController()
		{
		}

		public string FilePath { get; set; }

		public string FileName { get; set; }

		public string FileNameWithExtension => FileName + FileExtension;
		
		void IXmlExport.Export()
		{
			XmlExporter.WriteList(nameof(layers), layers);
		}

		public static implicit operator bool(AnimationController controller)
		{
			return controller != null;
		}

		public static AnimationController EmptyController()
		{
			AnimationController controller = new AnimationController();
			AnimationLayer baseLayer = AnimationLayer.CreateLayer("Base Layer");
			if (baseLayer == null)
			{
				Log.Error($"Unable to create base layer for controller.  AnimationController will be malformed.");
				return controller;
			}
			controller.layers.Add(baseLayer);
			return controller;
		}
	}
}
