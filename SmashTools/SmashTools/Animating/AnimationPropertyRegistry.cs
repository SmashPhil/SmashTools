using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using UnityEngine;
using static SmashTools.ConditionalPatch;

namespace SmashTools.Animations
{
	[StaticConstructorOnStartup]
	public static class AnimationPropertyRegistry
	{
		private static readonly Dictionary<Type, List<FieldInfo>> fieldRegistry = new Dictionary<Type, List<FieldInfo>>();
		private static readonly Dictionary<Type, List<PropertyInfo>> propertyRegistry = new Dictionary<Type, List<PropertyInfo>>();

		static AnimationPropertyRegistry()
		{
			RegisterType<Color>((typeof(float), nameof(Color.r)), (typeof(float), nameof(Color.g)), (typeof(float), nameof(Color.b)), (typeof(float), nameof(Color.a)));
			RegisterType<Vector3>((typeof(float), nameof(Vector3.x)), (typeof(float), nameof(Vector3.y)), (typeof(float), nameof(Vector3.z)));
		}

		public static List<AnimationPropertyContainer> GetAnimationProperties(this IAnimator animator)
		{
			List<AnimationPropertyContainer> result = new List<AnimationPropertyContainer>();

			GetAnimationProperties(animator, result);
			foreach (object obj in animator.ExtraAnimators)
			{
				GetAnimationProperties(obj, result);
			}

			return result;
		}

		private static void GetAnimationProperties(object parent, List<AnimationPropertyContainer> result)
		{
			Type type = parent.GetType();
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (fieldInfo.TryGetAttribute<AnimationPropertyAttribute>(out var animationPropertyAttribute))
				{
					if (!HandlesType(fieldInfo.FieldType))
					{
						Log.Error($"Type {fieldInfo.FieldType} is not supported as an animation property. It must be registered in the AnimationPropertyRegistry.");
						continue;
					}
					string name = animationPropertyAttribute.Name;
					if (name.NullOrEmpty())
					{
						name = fieldInfo.Name;
					}
					if (IsSupportedPrimitive(fieldInfo.FieldType))
					{
						AnimationProperty property = AnimationProperty.Create(name, fieldInfo);
						AnimationPropertyContainer container = new AnimationPropertyContainer(name, parent);
						container.Single = property;
						result.Add(container);
					}
					else if (IsContainerProperty(fieldInfo.FieldType))
					{
						object fieldParent = fieldInfo.GetValue(parent);
						AnimationPropertyContainer container = new AnimationPropertyContainer(name, fieldParent);
						foreach (FieldInfo innerFieldInfo in fieldInfo.FieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
						{
							if (!HandlesType(innerFieldInfo.FieldType) || !IsSupportedPrimitive(innerFieldInfo.FieldType))
							{
								Log.Error($"Type {innerFieldInfo.FieldType} is not supported as an animation property. Nested fields must be a supported primitive type {{ int, float, bool }}");
								continue;
							}
							AnimationProperty property = AnimationProperty.Create(fieldInfo.Name, innerFieldInfo);
							container.Children.Add(property);
						}
						result.Add(container);
					}
				}
			}
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
						string name = animationPropertyAttribute.Name;
						if (name.NullOrEmpty())
						{
							name = propertyInfo.Name;
						}
						AnimationProperty property = AnimationProperty.Create(name, propertyInfo);
						AnimationPropertyContainer container = new AnimationPropertyContainer(name, type);
						container.Single = property;
						result.Add(container);
					}
				}
			}
		}

		private static bool IsSupportedPrimitive(Type type)
		{
			return type == typeof(float) || type == typeof(int) || type == typeof(bool);
		}

		private static bool IsContainerProperty(Type type)
		{
			return fieldRegistry.ContainsKey(type) || propertyRegistry.ContainsKey(type);
		}

		public static bool HandlesType<T>()
		{
			return HandlesType(typeof(T));
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

			foreach ((Type type, string name) in properties)
			{
				FieldInfo fieldInfo = AccessTools.Field(typeof(T), name);
				if (fieldInfo != null)
				{
					fieldRegistry.AddOrInsert(typeof(T), fieldInfo);
				}
				else
				{
					PropertyInfo propertyInfo = AccessTools.Property(type, name);
					if (propertyInfo != null)
					{
						if (!propertyInfo.CanRead || !propertyInfo.CanWrite) //Properties must have both a getter and setter in order to be usable for animations
						{
							propertyRegistry.AddOrInsert(typeof(T), propertyInfo);
						}
						else
						{
							Log.Error($"{typeof(T)}::{name} property must have both a getter and setter in order to be used as an animation property.");
						}
					}
					else
					{
						Log.Error($"Unable to locate {typeof(T)}::{name}.");
					}
				}
			}
		}
	}
}
