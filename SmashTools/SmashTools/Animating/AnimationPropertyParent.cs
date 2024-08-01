using SmashTools.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationPropertyParent : IXmlExport, IEnumerable<AnimationProperty>
	{
		private string name;
		private Type type;

		private AnimationProperty single;
		private List<AnimationProperty> children = new List<AnimationProperty>();

		public AnimationPropertyParent()
		{
		}

		public AnimationPropertyParent(string name, Type type)
		{
			this.name = name;
			this.type = type;
		}

		public string Name => name;

		public Type Type => type;

		public AnimationProperty Single { get => single; internal set => single = value; }

		public List<AnimationProperty> Children => children;

		public bool IsValid => Single != null || !Children.NullOrEmpty();

		public AnimationProperty Current => throw new NotImplementedException();

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

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
			if (single != null)
			{
				XmlExporter.WriteElement(nameof(single), single);
			}
			if (!children.NullOrEmpty())
			{
				XmlExporter.WriteList(nameof(children), children);
			}
		}

		public IEnumerator<AnimationProperty> GetEnumerator()
		{
			if (Single != null)
			{
				yield return single;
			}
			else if (!Children.NullOrEmpty())
			{
				foreach (AnimationProperty animationProperty in Children)
				{
					yield return animationProperty;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
