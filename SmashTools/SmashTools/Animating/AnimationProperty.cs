using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationProperty
	{
		private string name;
		private Type type;

		[Unsaved]
		private MemberInfo memberInfo;

		public AnimationCurve curve = new AnimationCurve();

		public delegate void SetProperty(object parent, object value);

		public AnimationProperty(string name, Type type, MemberInfo memberInfo)
		{
			this.name = name;
			this.type = type;

			this.memberInfo = memberInfo;
		}

		public string Name => name;

		public PropertyType Type {get; internal set; }

		public bool IsValid => curve != null;

		public void WriteData()
		{
			Scribe_Values.Look(ref name, nameof(name));
			Scribe_Values.Look(ref type, nameof(type));
			Scribe_Deep.Look(ref curve, nameof(curve));

			curve ??= new AnimationCurve(); //Ensure animation curve is never null after scribe process
		}

		public static AnimationProperty Create(string name, FieldInfo fieldInfo)
		{
			AnimationProperty animationProperty = new AnimationProperty(name, fieldInfo.FieldType, fieldInfo);
			animationProperty.Type = PropertyTypeFrom(fieldInfo.FieldType);

			return animationProperty;
		}

		public static AnimationProperty Create(string name, PropertyInfo propertyInfo)
		{
			AnimationProperty animationProperty = new AnimationProperty(name, propertyInfo.PropertyType, propertyInfo);
			animationProperty.Type = PropertyTypeFrom(propertyInfo.PropertyType);

			return animationProperty;
		}

		//private static SetProperty GetAssignmentDelegate(Type type, MemberInfo memberInfo)
		//{
		//	DynamicMethod method;
		//	if (type.IsValueType)
		//	{
		//		method = new DynamicMethod("set", typeof(void), new Type[] { typeof(object), typeof(object) }, type, true);
		//	}
		//	else
		//	{
		//		method = new DynamicMethod("set", typeof(void), new Type[] { typeof(object), typeof(object) }, type, true);
		//	}
		//	ILGenerator ilg = method.GetILGenerator();
			
		//	ilg.Emit(OpCodes.Ldarg_0);
		//	ilg.Emit(OpCodes.Unbox, type);
		//	ilg.Emit(OpCodes.Ldarg_1);
		//}

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
