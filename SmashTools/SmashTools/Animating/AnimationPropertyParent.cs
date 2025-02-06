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
		private string identifier;
		private string label;
		private string name;
		private Type type;

		private AnimationProperty single;
		private List<AnimationProperty> children = new List<AnimationProperty>();

		public AnimationPropertyParent()
		{
		}

		private AnimationPropertyParent(string identifier, string label, string name, Type type)
		{
			this.identifier = identifier;
			this.label = label;
			this.name = name;
			this.type = type;
		}

		public string Identifier => identifier;

		public string Label => label;

		public string Name => name;

		public Type Type => type;

		public AnimationProperty Single { get => single; internal set => single = value; }

		public List<AnimationProperty> Children => children;

		public bool IsValid => Single != null || !Children.NullOrEmpty();

		public AnimationProperty Current => throw new NotImplementedException();

		public void EvaluateFrame(IAnimator animator, int frame)
		{
			if (Single != null)
			{
				Single.Evaluate(animator, frame);
			}
			else
			{
				for (int i = 0; i < Children.Count; i++)
				{
					Children[i].Evaluate(animator, frame);
				}
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

		internal void ResolveReferences()
		{
			if (Single != null)
			{
				Single.ResolveReferences();
			}
			else if (!Children.NullOrEmpty())
			{
				foreach (AnimationProperty property in Children)
				{
					property.ResolveReferences();
				}
			}
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(identifier), identifier);
			XmlExporter.WriteElement(nameof(label), label);
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
			if (single != null)
			{
				XmlExporter.WriteElement(nameof(single), single);
			}
			if (!children.NullOrEmpty())
			{
				XmlExporter.WriteCollection(nameof(children), children);
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

		public static AnimationPropertyParent Create(string identifier, string label, FieldInfo fieldInfo)
		{
			return new AnimationPropertyParent(identifier, label, fieldInfo.Name, fieldInfo.DeclaringType);
		}
	}
}
