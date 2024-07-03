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

		public bool IsValid => Single != null || !Children.NullOrEmpty();

		public void WriteData()
		{
			if (IsValid)
			{
				XmlExporter.WriteElement(nameof(name), name);
				XmlExporter.WriteElement(nameof(type), type.FullName);
			}
		}

		public bool AllKeyFramesAt(int frame)
		{
			if (Single != null)
			{
				return Single.curve.KeyFrameAt(frame);
			}
			else if (!Children.NullOrEmpty())
			{
				foreach (AnimationProperty property in Children)
				{
					if (!property.curve.KeyFrameAt(frame))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		public bool AnyKeyFrameAt(int frame)
		{
			if (Single != null)
			{
				return Single.curve.KeyFrameAt(frame);
			}
			else if (!Children.NullOrEmpty())
			{
				foreach (AnimationProperty property in Children)
				{
					if (property.curve.KeyFrameAt(frame))
					{
						return true;
					}
				}
				return false;
			}
			return false;
		}
	}
}
