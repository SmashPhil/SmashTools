using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	public class AnimationPropertyContainer
	{
		private readonly string name;
		private readonly Type parentType;
		private readonly object parent;

		public AnimationPropertyContainer(string name, object parent)
		{
			this.name = name;
			this.parent = parent;

			parentType = parent.GetType();
		}

		public string Name => name;

		public object Parent => parent;

		public Type ContainerType => parentType;

		public AnimationProperty Single { get; internal set; }

		public List<AnimationProperty> Children { get; private set; } = new List<AnimationProperty>();
	}
}
