using System.Collections.Generic;
using System.Linq;
using SmashTools.Xml;
using static SmashTools.Debug;

namespace SmashTools.Animations
{
	public class AnimationController : IAnimationFile
	{
		public const string DefaultControllerName = "New-Controller";
		public const string FileExtension = ".ctlr";

		public List<AnimationParameter> parameters = new List<AnimationParameter>();
		public List<AnimationLayer> layers = new List<AnimationLayer>();

		public string FilePath { get; set; }

		public string FileName { get; set; }

		public string FileNameWithExtension => FileName + FileExtension;

		public void AddLayer(string name)
		{
			name = AnimationLoader.GetAvailableName(layers.Select(layer => layer.name), name);
			AnimationLayer layer = AnimationLayer.CreateLayer(name);
			layer.Controller = this;
			Assert(layer != null, "Layer null");
			layers.Add(layer);
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteCollection(nameof(parameters), parameters);
			XmlExporter.WriteCollection(nameof(layers), layers);
		}

		void IAnimationFile.PostLoad()
		{
			foreach (AnimationLayer layer in layers)
			{
				layer.Controller = this;
				layer.PostLoad();
			}
		}

		public static implicit operator bool(AnimationController controller)
		{
			return controller != null;
		}

		public static AnimationController EmptyController()
		{
			AnimationController controller = new AnimationController();
			controller.FileName = DefaultControllerName;
			controller.AddLayer("Base Layer");
			return controller;
		}
	}
}
