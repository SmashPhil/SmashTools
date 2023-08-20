using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using UnityEngine;

namespace SmashTools
{
	public class EditWindow_TweakFields : EditWindow
	{
		private const float VectorLabelProportion = 0.5f;
		private const float VectorSubLabelProportion = 0.15f;

		private Thing thing;
		private static List<TweakInfo> tweakValueFields;

		private Vector2 scrollPosition;
		private Listing_SplitColumns listing = new Listing_SplitColumns();

		private static readonly Dictionary<FieldInfo, (string category, UISettingsType settingsType)> registeredFields = new Dictionary<FieldInfo, (string category, UISettingsType settingsType)>();

		public override bool IsDebug => true;

		public EditWindow_TweakFields(Thing thing)
		{
			this.thing = thing;
			optionalTitle = "TweakValues";
			tweakValueFields = FindAllTweakablesRecursive(thing).OrderBy(info => info.category).ThenBy(info => info.fieldInfo.DeclaringType.Name).ThenBy(info => info.Name).ToList();
		}

		public override Vector2 InitialSize => new Vector2(650, 650);

		public static bool RegisterField(FieldInfo fieldInfo, string category, UISettingsType settingsType)
		{
			if (registeredFields.ContainsKey(fieldInfo))
			{
				return false;
			}
			registeredFields.Add(fieldInfo, (category, settingsType));
			return true;
		}

		private IEnumerable<TweakInfo> FindAllTweakablesRecursive(Thing thing)
		{
			foreach (var info in FindAllTweakablesRecursive(thing.GetType(), thing, thing.Label))
			{
				yield return info;
			}
			foreach (var info in FindAllTweakablesRecursive(thing.def.GetType(), thing.def, thing.def.defName))
			{
				yield return info;
			}
			if (thing is ThingWithComps thingWithComps)
			{
				foreach (ThingComp thingComp in thingWithComps.AllComps)
				{
					foreach (var info in FindAllTweakablesRecursive(thingComp.GetType(), thingComp, thingComp.GetType().Name))
					{
						yield return info;
					}
				}
			}
		}

		private IEnumerable<TweakInfo> FindAllTweakablesRecursive(Type type, object parent, string category)
		{
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
			{
				if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is TweakFieldAttribute || registeredFields.ContainsKey(fieldInfo))
				{
					if (fieldInfo.IsStatic)
					{
						Log.Error($"Cannot use TweakFieldAttribute on static fields. Use vanilla's TweakValues for static fields instead. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
					}
					if (fieldInfo.IsLiteral)
					{
						Log.Error($"Cannot use TweakFieldAttribute on constants. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
					}
					if (fieldInfo.FieldType.IsClass)
					{
						if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
						{
							if (!fieldInfo.FieldType.GetGenericArguments()[0].IsClass)
							{
								Log.Error($"Cannot use TweakFieldAttribute on list of non-reference types. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
							}
							else
							{
								IList list = (IList)fieldInfo.GetValue(parent);
								int index = 0;
								foreach (object item in list)
								{
									List<TweakInfo> infos = FindAllTweakablesRecursive(item.GetType(), item, category).ToList();
									for (int i = 0; i < infos.Count; i++)
									{
										TweakInfo info = infos[i];
										if (list.Count > 1)
										{
											info.IndexInList = index;
										}
										yield return info;
									}
									index++;
								}
							}
						}
						else
						{
							object instance = fieldInfo.GetValue(parent);
							if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is TweakFieldAttribute tweakFieldAttribute && !tweakFieldAttribute.Category.NullOrEmpty())
							{
								category = tweakFieldAttribute.Category;
							}
							else if (registeredFields.TryGetValue(fieldInfo, out (string category, UISettingsType settingsType) info))
							{
								category = info.category;
							}
							foreach (TweakInfo info in FindAllTweakablesRecursive(fieldInfo.FieldType, instance, category))
							{
								yield return info;
							}
						}
					}
					else
					{
						UISettingsType settingsType = UISettingsType.None;
						if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is TweakFieldAttribute tweakFieldAttribute)
						{
							settingsType = tweakFieldAttribute.SettingsType;
						}
						else if (registeredFields.TryGetValue(fieldInfo, out var info))
						{
							settingsType = info.settingsType;
						}
						yield return CreateInfo(fieldInfo, parent, category, settingsType);
					}
				}
			}
		}

		private TweakInfo CreateInfo(FieldInfo fieldInfo, object instance, string category, UISettingsType settingsType)
		{
			TweakInfo info = new TweakInfo()
			{
				fieldInfo = fieldInfo,
				instance = instance,
				category = category,
				settingsType = settingsType,
				initialValue = fieldInfo.GetValue(instance),
			};
			return info;
		}

		public override void DoWindowContents(Rect inRect)
		{
			GUIState.Push();
			{
				Text.Font = GameFont.Small;
				Rect rect;
				Rect outRect = rect = inRect.ContractedBy(4f);
				rect.xMax -= 33f;
				Rect viewRect = new Rect(0f, 0f, rect.width, Text.LineHeight * tweakValueFields.Count).ContractedBy(4);
				listing.BeginScrollView(outRect, ref scrollPosition, ref viewRect, 2);
				{
					string currentCategory = string.Empty;
					foreach (TweakInfo info in tweakValueFields)
					{
						if (info.category != currentCategory)
						{
							currentCategory = info.category;
							//string label = headerTitleAttribute.Translate ? headerTitleAttribute.Label.Translate().ToString() : headerTitleAttribute.Label;
							listing.Header(currentCategory, ListingExtension.BannerColor, fontSize: GameFont.Small, anchor: TextAnchor.MiddleCenter, rowGap: 24);
						}
						DrawField(info);
					}
				}
				listing.EndScrollView(ref viewRect);
			}
			GUIState.Pop();
		}

		private void DrawField(TweakInfo info)
		{
			UISettingsType settingsType = info.settingsType;
			switch (info.settingsType)
			{
				case UISettingsType.None:
					return;
				case UISettingsType.Checkbox:
					bool checkOn = info.GetValue<bool>();
					bool checkAfter = checkOn;
					listing.CheckboxLabeled(info.Name, ref checkAfter, string.Empty, string.Empty, false);
					if (checkOn != checkAfter)
					{
						info.SetValue(checkAfter);
					}
					break;
				case UISettingsType.IntegerBox:
					{
						int value = info.GetValue<int>();
						int newValue = value;
						if (info.fieldInfo.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
						{
							listing.IntegerBox(info.Name, ref newValue, string.Empty, string.Empty, min: Mathf.RoundToInt(inputBox.MinValue), max: Mathf.RoundToInt(inputBox.MaxValue), lineHeight: 24);
						}
						else
						{
							listing.IntegerBox(info.Name, ref newValue, string.Empty, string.Empty, min: 0, max: int.MaxValue, lineHeight: 24);
						}
						if (value != newValue)
						{
							info.SetValue(newValue);
						}
						break;
					}
				case UISettingsType.FloatBox:
					{
						if (info.fieldInfo.FieldType == typeof(Vector2?))
						{
							Vector2? nullableValue = info.GetValue<Vector2?>();
							if (nullableValue.HasValue)
							{
								Vector2 newValue = nullableValue.Value;
								listing.Vector2Box(info.Name, ref newValue, labelProportion: VectorLabelProportion, subLabelProportions: VectorSubLabelProportion, buffer: 5);
								if (nullableValue.Value != newValue)
								{
									info.SetValue(newValue);
								}
							}
						}
						else if (info.fieldInfo.FieldType == typeof(Vector2))
						{
							Vector2 value = info.GetValue<Vector2>();
							Vector2 newValue = value;
							listing.Vector2Box(info.Name, ref newValue, labelProportion: VectorLabelProportion, subLabelProportions: VectorSubLabelProportion, buffer: 5);
							if (value != newValue)
							{
								info.SetValue(newValue);
							}
						}
						else if (info.fieldInfo.FieldType == typeof(Vector3?))
						{
							Vector3? nullableValue = info.GetValue<Vector3?>();
							if (nullableValue.HasValue)
							{
								Vector3 newValue = nullableValue.Value;
								listing.Vector3Box(info.Name, ref newValue, labelProportion: VectorLabelProportion, subLabelProportions: VectorSubLabelProportion, buffer: 5);
								if (nullableValue.Value != newValue)
								{
									info.SetValue(newValue);
								}
							}
						}
						else if (info.fieldInfo.FieldType == typeof(Vector3))
						{
							Vector3 value = info.GetValue<Vector3>();
							Vector3 newValue = value;
							listing.Vector3Box(info.Name, ref newValue, labelProportion: VectorLabelProportion, subLabelProportions: VectorSubLabelProportion, buffer: 5);
							if (value != newValue)
							{
								info.SetValue(newValue);
							}
						}
						else
						{
							float value = info.GetValue<float>();
							float newValue = value;
							if (info.fieldInfo.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
							{
								listing.FloatBox(info.Name, ref newValue, string.Empty, string.Empty, min: inputBox.MinValue, max: inputBox.MaxValue, lineHeight: 24);
							}
							else
							{
								listing.FloatBox(info.Name, ref newValue, string.Empty, string.Empty, min: 0, max: float.MaxValue, lineHeight: 24);
							}
							if (value != newValue)
							{
								info.SetValue(newValue);
							}
						}
						break;
					}
				case UISettingsType.ToggleLabel:
					break;
				case UISettingsType.SliderEnum:
					{
						int value = info.GetValue<int>();
						int newValue = value;
						listing.EnumSliderLabeled(info.Name, ref newValue, string.Empty, string.Empty, info.fieldInfo.FieldType);
						if (value != newValue)
						{
							info.SetValue(newValue);
						}
					}
					break;
				case UISettingsType.SliderInt:
					{
						int value = info.GetValue<int>();
						int newValue = value;
						if (info.fieldInfo.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty, slider.EndSymbol, Mathf.RoundToInt(slider.MinValue), Mathf.RoundToInt(slider.MaxValue), endValue: (int)slider.EndValue, maxValueDisplay: slider.MaxValueDisplay, minValueDisplay: slider.MinValueDisplay);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared {info.fieldInfo.DeclaringType}.{info.fieldInfo.Name} with no SliderValues attribute. Slider will use default values instead.", info.fieldInfo.GetHashCode());
							listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty, string.Empty, 0, 100);
						}
						if (value != newValue)
						{
							info.SetValue(newValue);
						}
					}
					break;
				case UISettingsType.SliderFloat:
					{
						float value = info.GetValue<float>();
						float newValue = value;
						if (info.fieldInfo.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty, slider.EndSymbol, slider.MinValue, slider.MaxValue, decimalPlaces: slider.RoundDecimalPlaces, endValue: slider.EndValue, increment: slider.Increment);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared {info.fieldInfo.DeclaringType}.{info.fieldInfo.Name} with no SliderValues attribute. Slider will use default values instead.", info.fieldInfo.GetHashCode());
							listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty, string.Empty, 0, 100);
						}
						if (value != newValue)
						{
							info.SetValue(newValue);
						}
					}
					break;
				case UISettingsType.SliderPercent:
					{
						float value = info.GetValue<float>();
						float newValue = value;
						if (info.fieldInfo.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							listing.SliderPercentLabeled(info.Name, ref newValue, string.Empty, string.Empty, slider.EndSymbol, slider.MinValue, slider.MaxValue, decimalPlaces: slider.RoundDecimalPlaces, endValue: slider.EndValue);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared {info.fieldInfo.DeclaringType}.{info.fieldInfo.Name} with no SliderValues attribute. Slider will use default values instead.", info.fieldInfo.GetHashCode());
							listing.SliderPercentLabeled(info.Name, ref newValue, string.Empty, string.Empty, "%", 0, 1, decimalPlaces: 0);
						}
						if (value != newValue)
						{
							info.SetValue(newValue);
						}
					}
					break;
				default:
					Log.ErrorOnce($"{settingsType} has not yet been implemented for PostToSettings.DrawLister. Please notify SmashPhil.", settingsType.ToString().GetHashCode());
					break;
			}
		}

		private struct TweakInfo
		{
			public FieldInfo fieldInfo;
			public object instance;

			public string category;

			public UISettingsType settingsType;

			public object initialValue;

			public string Name => IndexInList > 0 ? $"{fieldInfo.Name}_{IndexInList}" : fieldInfo.Name;

			public int IndexInList { get; internal set; }

			public T GetValue<T>()
			{
				return (T)fieldInfo.GetValue(instance);
			}

			public void SetValue<T>(T value)
			{
				fieldInfo.SetValue(instance, value);
			}
		}
	}
}
