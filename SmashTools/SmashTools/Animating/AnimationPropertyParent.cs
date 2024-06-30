using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	public class AnimationPropertyParent
	{
		private readonly string name;
		private readonly Type type;

		public AnimationPropertyParent(string name, Type type)
		{
			this.name = name;
			this.type = type;
		}

		public string Name => name;

		public Type Type => type;

		public object Parent { get; internal set; }

		public AnimationProperty Single { get; internal set; }

		public List<AnimationProperty> Children { get; private set; } = new List<AnimationProperty>();

		public void WriteData()
		{
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteElement(nameof(type), type.FullName);
		}
	}
}
