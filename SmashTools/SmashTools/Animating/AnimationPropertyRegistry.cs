#define DISABLE_PROPERTY_ANIMATIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using UnityEngine;

namespace SmashTools.Animations
{
	public static class AnimationPropertyRegistry
	{
		private static readonly Dictionary<Type, List<AnimationPropertyParent>> cachedProperties = new Dictionary<Type, List<AnimationPropertyParent>>();

		private static readonly Dictionary<Type, List<FieldInfo>> fieldRegistry = new Dictionary<Type, List<FieldInfo>>();
		private static readonly Dictionary<Type, List<PropertyInfo>> propertyRegistry = new Dictionary<Type, List<PropertyInfo>>();

		// Faster type look-up and handles UnityEngine types which are not parseable by RimWorld
		private static readonly Dictionary<string, Type> typeNames = new Dictionary<string, Type>();

		static AnimationPropertyRegistry()
		{
			RegisterType<Color>((typeof(float), nameof(Color.r)), (typeof(float), nameof(Color.g)), (typeof(float), nameof(Color.b)), (typeof(float), nameof(Color.a)));
			RegisterType<Vector2>((typeof(float), nameof(Vector2.x)), (typeof(float), nameof(Vector2.y)));
			RegisterType<Vector3>((typeof(float), nameof(Vector3.x)), (typeof(float), nameof(Vector3.y)), (typeof(float), nameof(Vector3.z)));
			RegisterType<IntVec2>((typeof(int), nameof(IntVec2.x)), (typeof(int), nameof(IntVec2.z)));
			RegisterType<IntVec3>((typeof(int), nameof(IntVec3.x)), (typeof(int), nameof(IntVec3.y)), (typeof(int), nameof(IntVec3.z)));
		}

		public static bool CachedTypeByName(string name, out Type type)
		{
			return typeNames.TryGetValue(name, out type);
		}

		public static List<AnimationPropertyParent> GetAnimationProperties(this IAnimator animator)
		{
			if (!cachedProperties.TryGetValue(animator.GetType(), out List<AnimationPropertyParent> result))
			{
				result = new List<AnimationPropertyParent>();
				GetAnimationProperties(animator, result);
				//foreach (object obj in animator.ExtraAnimators)
				//{
				//	GetAnimationProperties(obj, result);
				//}
				cachedProperties.Add(animator.GetType(), result);
			}
			return result;
		}

		/// <param name="parent">Object fields are being retrieved from</param>
		/// <param name="fieldInfo"></param>
		/// <param name="result"></param>
		private static void GetAnimationProperties(IAnimator animator, List<AnimationPropertyParent> result)
		{
			Type type = animator.GetType();
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (fieldInfo.TryGetAttribute<AnimationPropertyAttribute>(out var animationPropertyAttribute))
				{
					if (!HandlesType(fieldInfo.FieldType))
					{
						Log.Error($"Type {fieldInfo.FieldType} is not supported as an animation property. It must be registered in the AnimationPropertyRegistry.");
						continue;
					}
					string label = animationPropertyAttribute.Name;
					if (label.NullOrEmpty())
					{
						label = fieldInfo.Name;
					}
					AnimationPropertyParent container = AnimationPropertyParent.Create(type.Name, label, fieldInfo);
					if (IsSupportedPrimitive(fieldInfo.FieldType))
					{
						AnimationProperty property = AnimationProperty.Create(type, label, fieldInfo);
						container.Single = property;
						result.Add(container);
					}
					else if (IsContainerProperty(fieldInfo.FieldType))
					{
						foreach (FieldInfo innerFieldInfo in fieldInfo.FieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
						{
							if (!HandlesType(innerFieldInfo.FieldType) || !IsSupportedPrimitive(innerFieldInfo.FieldType))
							{
								Log.Error($"Type {innerFieldInfo.FieldType} is not supported as an animation property. Nested fields must be a supported primitive type {{ int, float, bool }}");
								continue;
							}
							AnimationProperty property = AnimationProperty.Create(type, innerFieldInfo.Name, innerFieldInfo, fieldInfo);
							container.Children.Add(property);
						}
						result.Add(container);
					}
				}
			}
#if !DISABLE_PROPERTY_ANIMATIONS
			foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (propertyInfo.TryGetAttribute<AnimationPropertyAttribute>(out var animationPropertyAttribute))
				{
					if (!HandlesType(propertyInfo.PropertyType))
					{
						Log.Error($"{propertyInfo.PropertyType} is not a supported type. It must be registered in the AnimationPropertyRegistry so inner values can be fetched.");
						continue;
					}
					if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
					{
						Log.Error($"{propertyInfo.PropertyType}::{propertyInfo.Name} must have both a getter and a setter in order to support being used as an animation property.");
						continue;
					}
					if (IsSupportedPrimitive(propertyInfo.PropertyType))
					{
						string label = animationPropertyAttribute.Name;
						if (label.NullOrEmpty())
						{
							label = propertyInfo.Name;
						}
						AnimationProperty property = AnimationProperty.Create(label, propertyInfo);
						AnimationPropertyParent container = AnimationPropertyParent.Create(type.Name, label, propertyInfo);
						container.Single = property;
						result.Add(container);
					}
				}
			}
#endif
		}

		private static bool IsSupportedPrimitive(Type type)
		{
			return type == typeof(float) || type == typeof(int) || type == typeof(bool);
		}

		private static bool IsContainerProperty(Type type)
		{
			return fieldRegistry.ContainsKey(type) || propertyRegistry.ContainsKey(type);
		}

		public static bool HandlesType(Type type)
		{
			if (IsSupportedPrimitive(type))
			{
				return true;
			}
			return fieldRegistry.ContainsKey(type) || propertyRegistry.ContainsKey(type);
		}

		public static void RegisterType<T>(params (Type type, string name)[] properties)
		{
			if (properties.NullOrEmpty())
			{
				Log.Error($"Trying to register AnimationProperty in registry with no properties.");
				return;
			}
			typeNames[GenTypes.GetTypeNameWithoutIgnoredNamespaces(typeof(T))] = typeof(T);
			foreach ((Type type, string name) in properties)
			{
				FieldInfo fieldInfo = AccessTools.Field(typeof(T), name);
				if (fieldInfo != null)
				{
					fieldRegistry.AddOrInsert(typeof(T), fieldInfo);
					return;
				}
#if !DISABLE_PROPERTY_ANIMATIONS
				PropertyInfo propertyInfo = AccessTools.Property(type, name);
				if (propertyInfo != null)
				{
					if (!propertyInfo.CanRead || !propertyInfo.CanWrite) //Properties must have both a getter and setter in order to be usable for animations
					{
						propertyRegistry.AddOrInsert(typeof(T), propertyInfo);
					}
					else
					{
						Log.Error($"{typeof(T)}.{name} property must have both a getter and setter in order to be used as an animation property.");
					}
					return;
				}
#endif
				Log.Error($"Unable to locate {typeof(T)}.{name}.");
			}
		}
	}
}
