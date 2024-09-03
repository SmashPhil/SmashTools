using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SmashTools.Xml;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationProperty : IXmlExport
	{
		private readonly string label;
		private readonly string name;
		private readonly Type type;
		private PropertyType propertyType;

		public AnimationCurve curve = new AnimationCurve();

		[Unsaved]
		private Color color;
		[Unsaved]
		public SetValue setValue;

		public delegate void SetValue(ref object parent, float frame);

		public AnimationProperty()
		{
		}

		private AnimationProperty(string label, string name, Type type)
		{
			this.label = label;
			this.name = name;
			this.type = type;
		}

		public string Label => label;

		public string Name => name;

		public Type Type => type;

		public PropertyType PropType => propertyType;

		public bool IsValid => curve != null;

		public Color Color
		{
			get
			{
				if (color == Color.clear)
				{
					color = UnityEngine.Random.ColorHSV(0, 1, 1, 1, 0.5f, 1);
				}
				return color;
			}
		}

		//public void Set<T>(T parent, int frame) => setValue.Invoke(parent, frame);

		public void Set<T>(ref T parent, int frame)
		{
		}

		public static AnimationProperty Create(string label, FieldInfo fieldInfo)
		{
			AnimationProperty animationProperty = new AnimationProperty(label, fieldInfo.Name, fieldInfo.DeclaringType);
			animationProperty.propertyType = PropertyTypeFrom(fieldInfo.FieldType);
			animationProperty.PostLoad();
			return animationProperty;
		}

		public static AnimationProperty Create(string label, PropertyInfo propertyInfo)
		{
			AnimationProperty animationProperty = new AnimationProperty(label, propertyInfo.Name, propertyInfo.DeclaringType);
			animationProperty.propertyType = PropertyTypeFrom(propertyInfo.PropertyType);
			animationProperty.PostLoad();
			return animationProperty;
		}

		internal void PostLoad()
		{
			Debug.Assert(propertyType > PropertyType.Invalid, "AnimationProperty has not been properly initialized");
			//setValue = GetDynamicMethod();
		}

		private SetValue GetDynamicMethod()
		{
			FieldInfo fieldInfo = AccessTools.Field(type, name);
			if (fieldInfo == null)
			{
				Log.Error($"Unable to load {type}.{name} for animation.");
				return null;
			}
			FieldInfo curveField = AccessTools.Field(typeof(AnimationProperty), nameof(curve));
			Debug.Assert(curveField != null, "AnimationProperty.curve is null");
			MethodInfo curveFunction = AccessTools.Method(typeof(AnimationCurve), nameof(AnimationCurve.Function));
			Debug.Assert(curveFunction != null, "AnimationCurve.Function is null");

			DynamicMethod method = new DynamicMethod("SetValueForProperty", 
				typeof(void), // Return type
				new Type[] { typeof(AnimationProperty), typeof(object).MakeByRefType(), typeof(float) }, // this*, parent, frame
				typeof(AnimationProperty).Module, // SmashTools.dll
				true); // Skip visibility checks

			ILGenerator ilg = method.GetILGenerator();

			// parent
			ilg.Emit(OpCodes.Ldarg_1);
			if (type.IsValueType)
			{
				ilg.Emit(OpCodes.Unbox_Any, type);
			}
			else
			{
				ilg.Emit(OpCodes.Castclass, type);
			}

			// this.curve.Function(frame)
			ilg.Emit(OpCodes.Ldarg_0);
			ilg.Emit(OpCodes.Ldfld, curveField);
			ilg.Emit(OpCodes.Ldarg_2); // frame
			ilg.Emit(OpCodes.Callvirt, curveFunction);

			switch (propertyType)
			{
				// (int)value
				case PropertyType.Int:
					Debug.Assert(fieldInfo.FieldType == typeof(int));
					ilg.Emit(OpCodes.Conv_I4);
					break;
				// value != 0
				case PropertyType.Bool:
					Debug.Assert(fieldInfo.FieldType == typeof(bool));
					ilg.Emit(OpCodes.Ldc_R4, 0f);
					ilg.Emit(OpCodes.Ceq);
					ilg.Emit(OpCodes.Ldc_I4_0);
					ilg.Emit(OpCodes.Ceq);
					break;
				default:
					Debug.Assert(fieldInfo.FieldType == typeof(float));
					break;
			}
			// parent.field = value
			ilg.Emit(OpCodes.Stfld, fieldInfo);
			ilg.Emit(OpCodes.Ret);
			return (SetValue)method.CreateDelegate(typeof(SetValue), this);
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

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(label), label);
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
			XmlExporter.WriteElement(nameof(propertyType), propertyType.ToString());
			XmlExporter.WriteElement(nameof(curve), curve);
		}

		public enum PropertyType
		{
			Invalid,
			Float,
			Int,
			Bool
		}
	}
}
