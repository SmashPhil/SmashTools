using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SmashTools.Debugging;
using SmashTools.Xml;
using UnityEngine;
using Verse;
using static SmashTools.Debug;

namespace SmashTools.Animations
{
	public class AnimationProperty : IXmlExport
	{
		// Treated as an array, but RimWorld is incapable of parsing in arrays so a fixed-capacity list is required.
		private List<ObjectPath> objectPath = new List<ObjectPath>();
		private string label;
		private string name;
		private PropertyType propertyType;

		// Strictly used for serialization, UnityEngine types are not supported by RimWorld's parser
		// so the types are resolved post-load w/ these strings.
		private string type;
		private string animatorType;

		public AnimationCurve curve = new AnimationCurve();

		[Unsaved]
		private Type loadedType;
		[Unsaved]
		private Type loadedAnimatorType;

		/// <summary>
		/// Color of property in animation curve tab
		/// </summary>
		[Unsaved]
		private Color color;
		/// <summary>
		/// Sets value of field given value from curve evaluation
		/// </summary>
		[Unsaved]
		private SetValue evaluateValue;
		/// <summary>
		/// Sets value of field passed in from function.
		/// </summary>
		/// <remarks>Note: Must be able to convert to field's type</remarks>
		[Unsaved]
		private SetValue setValue;
		/// <summary>
		/// Gets value of field from property instance
		/// </summary>
		[Unsaved]
		private GetValue getValue;

		public delegate float GetValue(IAnimator animator);
		public delegate void SetValue(IAnimator animator, float value);
		
		public AnimationProperty()
		{
		}

		private AnimationProperty(Type animatorType, string label, string name, Type type, FieldInfo[] fieldPath)
		{
			loadedAnimatorType = animatorType;
			this.label = label;
			this.name = name;
			loadedType = type;

			if (!fieldPath.NullOrEmpty())
			{
				objectPath = new List<ObjectPath>(fieldPath.Length);
				for (int i = 0; i < fieldPath.Length; i++)
				{
					FieldInfo fieldInfo = fieldPath[i];
					objectPath.Add(new ObjectPath(fieldInfo));
				}
			}
		}

		public string Label => label;

		public string Name => name;

		public Type Type => loadedType;

		public Type AnimatorType => loadedAnimatorType;

		public PropertyType PropType => propertyType;

		public bool IsValid => curve != null;

		internal GetValue GetProperty => getValue;

		internal SetValue SetProperty => setValue;

		public FieldInfo FieldInfo { get; private set; }

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

		public void Evaluate(IAnimator animator, int frame) => evaluateValue.Invoke(animator, frame);

		public static AnimationProperty Create(Type animatorType, string label, FieldInfo fieldInfo, params FieldInfo[] objectPath)
		{
			AnimationProperty animationProperty = new AnimationProperty(animatorType, label, fieldInfo.Name, fieldInfo.DeclaringType, objectPath);
			animationProperty.propertyType = PropertyTypeFrom(fieldInfo.FieldType);
			animationProperty.PostLoad();
			return animationProperty;
		}

		internal void PostLoad()
		{
			// Unity types are not parsed by RimWorld so must first check cached string -> type map.
			if (loadedType == null && !AnimationPropertyRegistry.CachedTypeByName(type, out loadedType))
			{
				loadedType = ParseHelper.ParseType(type);
			}
			if (loadedAnimatorType == null && !AnimationPropertyRegistry.CachedTypeByName(animatorType, out loadedAnimatorType))
			{
				loadedAnimatorType = ParseHelper.ParseType(animatorType);
			}
			
			try
			{
				Assert(propertyType > PropertyType.Invalid, "AnimationProperty has not been properly initialized");
				FieldInfo = AccessTools.Field(Type, name);
				Trace(FieldInfo != null, $"Unable to load {Type.Name}.{name} for animation.");
				if (FieldInfo != null)
				{
					GenerateEvaluateCurveMethod();
					GenerateSetValueMethod();
					GenerateGetValueMethod();
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception caught while generating dynamic methods. Exception={ex}");
			}
		}

		private void GenerateEvaluateCurveMethod()
		{
			FieldInfo curveField = AccessTools.Field(typeof(AnimationProperty), nameof(curve));
			Assert(curveField != null, "AnimationProperty.curve is null");
			MethodInfo curveFunction = AccessTools.Method(typeof(AnimationCurve), nameof(AnimationCurve.Function));
			Assert(curveFunction != null, "AnimationCurve.Function is null");

			DynamicMethod method = new DynamicMethod("EvaluateCurveForProperty", 
				typeof(void), // Return type
				new Type[] { typeof(AnimationProperty), typeof(IAnimator), typeof(float) }, // this*, parent, frame
				typeof(AnimationProperty).Module, // SmashTools.dll
				true); // Skip visibility checks

			ILGenerator ilg = method.GetILGenerator();

			// (T)animator
			ilg.Emit(OpCodes.Ldarg_1);
			ilg.Emit(OpCodes.Castclass, AnimatorType);

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
					Assert(FieldInfo.FieldType == typeof(int));
					ilg.Emit(OpCodes.Conv_I4);
					break;
				// value != 0
				case PropertyType.Bool:
					Assert(FieldInfo.FieldType == typeof(bool));
					ilg.Emit(OpCodes.Ldc_R4, 0f);
					ilg.Emit(OpCodes.Ceq);
					ilg.Emit(OpCodes.Ldc_I4_0);
					ilg.Emit(OpCodes.Ceq);
					break;
				default:
					Assert(FieldInfo.FieldType == typeof(float));
					break;
			}
			// parent.field = value
			ilg.Emit(OpCodes.Stfld, FieldInfo);
			ilg.Emit(OpCodes.Ret);
			evaluateValue = (SetValue)method.CreateDelegate(typeof(SetValue), this);
		}

		private void GenerateGetValueMethod()
		{
			DynamicMethod method = new DynamicMethod("GetValueForProperty",
				typeof(float), // Return type
				new Type[] { typeof(AnimationProperty), typeof(IAnimator) }, // this*, parent
				typeof(AnimationProperty).Module, // SmashTools.dll
				true); // Skip visibility checks

			ILGenerator ilg = method.GetILGenerator();

			// (T)animator
			ilg.Emit(OpCodes.Ldarg_1);
			ilg.Emit(OpCodes.Castclass, AnimatorType);
			
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

			// [parameter] value
			ilg.Emit(OpCodes.Ldfld, FieldInfo);

			switch (propertyType)
			{
				// (int)value
				case PropertyType.Int:
					Assert(FieldInfo.FieldType == typeof(int));
					ilg.Emit(OpCodes.Conv_R4);
					break;
				// value = bool ? 1f : 0f
				case PropertyType.Bool:
					Assert(FieldInfo.FieldType == typeof(bool));
					Label brTrueLabel = ilg.DefineLabel();
					Label brLabel = ilg.DefineLabel();
					ilg.Emit(OpCodes.Brtrue_S, brTrueLabel);
					ilg.Emit(OpCodes.Ldc_I4_0);
					ilg.Emit(OpCodes.Br_S, brLabel);
					ilg.MarkLabel(brTrueLabel);
					ilg.Emit(OpCodes.Ldc_I4_1);
					ilg.MarkLabel(brLabel);
					ilg.Emit(OpCodes.Conv_R4);
					break;
				default:
					Assert(FieldInfo.FieldType == typeof(float));
					break;
			}

			ilg.Emit(OpCodes.Ret);
			getValue = (GetValue)method.CreateDelegate(typeof(GetValue), this);
		}

		private void GenerateSetValueMethod()
		{
			DynamicMethod method = new DynamicMethod("SetValueForProperty",
				typeof(void), // Return type
				new Type[] { typeof(AnimationProperty), typeof(IAnimator), typeof(float) }, // this*, parent, value
				typeof(AnimationProperty).Module, // SmashTools.dll
				true); // Skip visibility checks

			ILGenerator ilg = method.GetILGenerator();

			// (T)animator
			ilg.Emit(OpCodes.Ldarg_1);
			ilg.Emit(OpCodes.Castclass, AnimatorType);

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

			// [parameter] value
			ilg.Emit(OpCodes.Ldarg_2);

			switch (propertyType)
			{
				// (int)value
				case PropertyType.Int:
					Assert(FieldInfo.FieldType == typeof(int));
					ilg.Emit(OpCodes.Conv_I4);
					break;
				// value != 0
				case PropertyType.Bool:
					Assert(FieldInfo.FieldType == typeof(bool));
					ilg.Emit(OpCodes.Ldc_R4, 0f);
					ilg.Emit(OpCodes.Ceq);
					ilg.Emit(OpCodes.Ldc_I4_0);
					ilg.Emit(OpCodes.Ceq);
					break;
				default:
					Assert(FieldInfo.FieldType == typeof(float));
					break;
			}
			// parent.field = value
			ilg.Emit(OpCodes.Stfld, FieldInfo);
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
			XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(Type));
			XmlExporter.WriteElement(nameof(animatorType), GenTypes.GetTypeNameWithoutIgnoredNamespaces(AnimatorType));
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
			private Type type;
			private string name;

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
