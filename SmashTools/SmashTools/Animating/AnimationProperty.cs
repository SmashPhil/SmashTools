using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SmashTools.Debugging;
using SmashTools.Xml;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationProperty : IXmlExport
	{
		private readonly ObjectPath[] objectPath;
		private readonly string label;
		private readonly string name;
		private readonly Type type;
		private readonly Type animatorType;
		private PropertyType propertyType;

		public AnimationCurve curve = new AnimationCurve();

		[Unsaved]
		private Color color;
		[Unsaved]
		public SetValue setValue;

		public delegate void SetValue(IAnimator animator, float frame);

		public AnimationProperty()
		{
		}

		private AnimationProperty(Type animatorType, string label, string name, Type type, FieldInfo[] fieldPath)
		{
			this.animatorType = animatorType;
			this.label = label;
			this.name = name;
			this.type = type;

			if (!fieldPath.NullOrEmpty())
			{
				objectPath = new ObjectPath[fieldPath.Length];
				for (int i = 0; i < fieldPath.Length; i++)
				{
					FieldInfo fieldInfo = fieldPath[i];
					objectPath[i] = new ObjectPath(fieldInfo);
				}
			}
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

		public void Set(IAnimator animator, int frame) => setValue.Invoke(animator, frame);

		public static AnimationProperty Create(Type animatorType, string label, FieldInfo fieldInfo, params FieldInfo[] objectPath)
		{
			AnimationProperty animationProperty = new AnimationProperty(animatorType, label, fieldInfo.Name, fieldInfo.DeclaringType, objectPath);
			animationProperty.propertyType = PropertyTypeFrom(fieldInfo.FieldType);
			animationProperty.PostLoad();
			return animationProperty;
		}

		public static AnimationProperty Create(Type animatorType, string label, PropertyInfo propertyInfo)
		{
			throw new NotSupportedException("Property modifying animations");
			//AnimationProperty animationProperty = new AnimationProperty(objectPath, label, propertyInfo.Name, propertyInfo.DeclaringType);
			//animationProperty.propertyType = PropertyTypeFrom(propertyInfo.PropertyType);
			//animationProperty.PostLoad();
			//return animationProperty;
		}

		internal void PostLoad()
		{
			Debug.Assert(propertyType > PropertyType.Invalid, "AnimationProperty has not been properly initialized");
			GenerateDynamicMethod();
		}

		private void GenerateDynamicMethod()
		{
			FieldInfo fieldInfo = AccessTools.Field(type, name);
			if (fieldInfo == null)
			{
				Log.Error($"Unable to load {type}.{name} for animation.");
				return;
			}
			FieldInfo curveField = AccessTools.Field(typeof(AnimationProperty), nameof(curve));
			Debug.Assert(curveField != null, "AnimationProperty.curve is null");
			MethodInfo curveFunction = AccessTools.Method(typeof(AnimationCurve), nameof(AnimationCurve.Function));
			Debug.Assert(curveFunction != null, "AnimationCurve.Function is null");

			DynamicMethod method = new DynamicMethod("SetValueForProperty", 
				typeof(void), // Return type
				new Type[] { typeof(AnimationProperty), typeof(IAnimator), typeof(float) }, // this*, parent, frame
				typeof(AnimationProperty).Module, // SmashTools.dll
				true); // Skip visibility checks

			ILGenerator ilg = method.GetILGenerator();

			// (T)animator
			ilg.Emit(OpCodes.Ldarg_1);
			ilg.Emit(OpCodes.Castclass, animatorType);

			if (!objectPath.NullOrEmpty())
			{
				foreach (ObjectPath path in objectPath)
				{
					FieldInfo field = path.FieldInfo;
					if (field.FieldType.IsValueType)
					{
						ilg.Emit(OpCodes.Ldflda, field);
					}
					else
					{
						ilg.Emit(OpCodes.Ldfld, field);
					}
				}
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
			setValue = (SetValue)method.CreateDelegate(typeof(SetValue), this);
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
			XmlExporter.WriteCollection(nameof(objectPath), objectPath);
			XmlExporter.WriteElement(nameof(label), label);
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
			XmlExporter.WriteElement(nameof(animatorType), GenTypes.GetTypeNameWithoutIgnoredNamespaces(animatorType));
			XmlExporter.WriteObject(nameof(propertyType), propertyType);
			XmlExporter.WriteElement(nameof(curve), curve);
		}

		public enum PropertyType
		{
			Invalid,
			Float,
			Int,
			Bool
		}

		public class ObjectPath : IXmlExport
		{
			private readonly Type type;
			private readonly string name;

			public ObjectPath()
			{
			}

			public ObjectPath(FieldInfo fieldPath)
			{
				type = fieldPath.DeclaringType;
				name = fieldPath.Name;
			}

			public FieldInfo FieldInfo => AccessTools.Field(type, name);

			void IXmlExport.Export()
			{
				XmlExporter.WriteObject(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
				XmlExporter.WriteElement(nameof(name), name);
			}
		}
	}
}
