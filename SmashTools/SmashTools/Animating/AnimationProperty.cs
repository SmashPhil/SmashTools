using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	public class AnimationProperty
	{
		private readonly string name;
		private readonly Type parentType;

		public LagrangeCurve curve;
		public List<AnimationBoolState> boolStates;

		public AnimationProperty(string name, Type parentType)
		{
			this.name = name;
			this.parentType = parentType;
		}

		public string Name => name;

		public PropertyType Type {get; internal set; }

		public object Parent { get; internal set; }

		public void WriteData()
		{

		}

		public static AnimationProperty Create(string name, FieldInfo fieldInfo)
		{
			AnimationProperty animationProperty = new AnimationProperty(name, fieldInfo.DeclaringType);
			animationProperty.Type = PropertyTypeFrom(fieldInfo.FieldType);

			return animationProperty;
		}

		public static AnimationProperty Create(string name, PropertyInfo propertyInfo)
		{
			AnimationProperty animationProperty = new AnimationProperty(name, propertyInfo.DeclaringType);
			animationProperty.Type = PropertyTypeFrom(propertyInfo.PropertyType);

			return animationProperty;
		}

		public static PropertyType PropertyTypeFrom(Type type)
		{
			if (type == typeof(float))
			{
				return PropertyType.Float;
			}
			if (type == typeof(int))
			{
				return PropertyType.Int;
			}
			if (type == typeof(bool))
			{
				return PropertyType.Bool;
			}
			throw new NotImplementedException($"{type} is not a supported PropertyType for keyframe-level animation properties.");
		}

		public enum PropertyType
		{
			Float,
			Int,
			Bool
		}
	}
}
