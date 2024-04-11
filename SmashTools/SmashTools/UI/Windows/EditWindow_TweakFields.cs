using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using UnityEngine;

namespace SmashTools
{
	public class EditWindow_TweakFields : EditWindow
	{
		private const float VectorLabelProportion = 0.5f;
		private const float VectorSubLabelProportion = 0.15f;
		private const float FloatRangeLabelProportion = 0.5f;
		private const float FloatRangeSubLabelProportion = 0.25f;

		private const float RowHeight = 24;
		private const int ColumnCount = 2;

		private Thing thing;
		private static List<TweakInfo> tweakValueFields;

		private Vector2 scrollPosition;
		private Listing_SplitColumns listing = new Listing_SplitColumns();

		private static readonly Dictionary<FieldInfo, (string category, string subCategory, UISettingsType settingsType)> registeredFields = new Dictionary<FieldInfo, (string category, string subCategory, UISettingsType settingsType)>();

		public override bool IsDebug => true;

		public EditWindow_TweakFields(Thing thing)
		{
			this.thing = thing;
			optionalTitle = "TweakValues";
			tweakValueFields = FindAllTweakablesRecursive(thing).OrderBy(info => info.category).ThenBy(info => info.subCategory).ThenBy(info => info.fieldInfo.DeclaringType.Name).ThenBy(info => info.Name).ToList();
			RecacheHeight();
		}

		public override Vector2 InitialSize => new Vector2(UI.screenWidth / 2, UI.screenHeight * 0.9f);

		private float CachedHeight { get; set; }

		public static bool RegisterField(FieldInfo fieldInfo, string category, string subCategory, UISettingsType settingsType)
		{
			if (registeredFields.ContainsKey(fieldInfo))
			{
				return false;
			}
			registeredFields.Add(fieldInfo, (category, subCategory, settingsType));
			return true;
		}

		private IEnumerable<TweakInfo> FindAllTweakablesRecursive(Thing thing)
		{
			foreach (var info in FindAllTweakablesRecursive(thing.def.GetType(), thing.def, thing.def.defName, string.Empty))
			{
				yield return info;
			}
			if (!thing.def.comps.NullOrEmpty())
			{
				foreach (CompProperties compProperties in thing.def.comps)
				{
					foreach (var info in FindAllTweakablesRecursive(compProperties.GetType(), compProperties, compProperties.GetType().Name, string.Empty))
					{
						yield return info;
					}
				}
			}
			foreach (var info in FindAllTweakablesRecursive(thing.GetType(), thing, thing.Label, string.Empty))
			{
				yield return info;
			}
			if (thing is ThingWithComps thingWithComps)
			{
				foreach (ThingComp thingComp in thingWithComps.AllComps)
				{
					foreach (var info in FindAllTweakablesRecursive(thingComp.GetType(), thingComp, thingComp.GetType().Name, string.Empty))
					{
						yield return info;
					}
				}
			}
		}

		private IEnumerable<TweakInfo> FindAllTweakablesRecursive(Type type, object parent, string category, string subCategory)
		{
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
			{
				if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is TweakFieldAttribute || registeredFields.ContainsKey(fieldInfo))
				{
					if (fieldInfo.IsStatic)
					{
						Log.Error($"Cannot use TweakFieldAttribute on static fields. Use vanilla's TweakValues for static fields instead. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
						continue;
					}
					if (fieldInfo.IsLiteral)
					{
						Log.Error($"Cannot use TweakFieldAttribute on constants. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
						continue;
					}
					if (fieldInfo.FieldType.IsClass)
					{
						if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
						{
							if (!fieldInfo.FieldType.GetGenericArguments()[0].IsClass)
							{
								Log.Error($"Cannot use TweakFieldAttribute on list of non-reference types. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
								continue;
							}
							else
							{
								IList list = (IList)fieldInfo.GetValue(parent);
								if (list != null)
								{
									int index = 0;
									foreach (object item in list)
									{
										(string newCategory, string newSubCategory) = GetCategory(fieldInfo, item, category, subCategory);
										List<TweakInfo> infos = FindAllTweakablesRecursive(item.GetType(), item, newCategory, newSubCategory).ToList();
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
						}
						else
						{
							object instance = fieldInfo.GetValue(parent);
							if (instance != null)
							{
								(string newCategory, string newSubCategory) = GetCategory(fieldInfo, instance, category, subCategory);
								foreach (TweakInfo info in FindAllTweakablesRecursive(fieldInfo.FieldType, instance, newCategory, newSubCategory))
								{
									yield return info;
								}
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
						yield return CreateInfo(fieldInfo, parent, category, subCategory, settingsType);
					}
				}
			}
		}

		private (string category, string subCategory) GetCategory(FieldInfo fieldInfo, object instance, string category, string subCategory)
		{
			if (instance is ITweakFields tweakFields)
			{
				if (!tweakFields.Category.NullOrEmpty())
				{
					category = tweakFields.Category;
				}
				if (!tweakFields.Label.NullOrEmpty())
				{
					subCategory = tweakFields.Label;
				}
			}
			else if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is TweakFieldAttribute tweakFieldAttribute)
			{
				if (!tweakFieldAttribute.Category.NullOrEmpty())
				{
					category = tweakFieldAttribute.Category;
				}
				if (!tweakFieldAttribute.SubCategory.NullOrEmpty())
				{
					subCategory = tweakFieldAttribute.SubCategory;
				}
			}
			else if (registeredFields.TryGetValue(fieldInfo, out (string category, string subCategory, UISettingsType settingsType) info))
			{
				if (!info.category.NullOrEmpty())
				{
					category = info.category;
				}
				if (!info.subCategory.NullOrEmpty())
				{
					subCategory = info.subCategory;
				}
			}
			return (category, subCategory);
		}

		private TweakInfo CreateInfo(FieldInfo fieldInfo, object instance, string category, string subCategory, UISettingsType settingsType)
		{
			TweakInfo info = new TweakInfo()
			{
				fieldInfo = fieldInfo,
				instance = instance,
				category = category,
				subCategory = subCategory,
				settingsType = settingsType,
				initialValue = fieldInfo.GetValue(instance),
			};
			info.IndexInList = -1; //Default to -1, 0 and above indicate it's in a list
			return info;
		}

		private void RecacheHeight()
		{
			CachedHeight = 0;

			GUIState.Push();
			{
				string currentCategory = string.Empty;
				string currentSubCategory = string.Empty;

				int rowCount = 0;
				foreach (TweakInfo info in tweakValueFields)
				{
					if (info.category != currentCategory)
					{
						currentCategory = info.category;
						if (!currentCategory.NullOrEmpty())
						{
							Text.Font = GameFont.Medium;
							float rowHeight = Text.LineHeight + 2; //+2 for gap
							CachedHeight += rowHeight;
							rowCount = 0;
						}
					}
					if (info.subCategory != currentSubCategory)
					{
						currentSubCategory = info.subCategory;
						if (!currentSubCategory.NullOrEmpty())
						{
							Text.Font = GameFont.Small;
							float rowHeight = Text.LineHeight + 2; //+2 for gap
							CachedHeight += rowHeight;
							rowCount = 0;
						}
					}
					if (DrawField(info, out _))
					{
						rowCount++;
						if (rowCount > ColumnCount) //Mark for new column
						{
							rowCount = 1;
						}
						if (rowCount == 1) //Only count for first item in row
						{
							CachedHeight += Listing_SplitColumns.GapHeight;
						}
					}
				}
			}
			GUIState.Pop();
		}

		public override void DoWindowContents(Rect inRect)
		{
			GUIState.Push();
			{
				Text.Font = GameFont.Small;
				Rect rect;
				Rect outRect = rect = inRect.ContractedBy(4f);
				rect.xMax -= 33f;
				Rect viewRect = new Rect(0f, 0f, rect.width, CachedHeight).ContractedBy(4);
				listing.BeginScrollView(outRect, ref scrollPosition, ref viewRect, ColumnCount);
				{
					string currentCategory = string.Empty;
					string currentSubCategory = string.Empty;
					foreach (TweakInfo info in tweakValueFields)
					{
						if (info.category != currentCategory)
						{
							currentCategory = info.category;
							if (!currentCategory.NullOrEmpty())
							{
								listing.Header(currentCategory, ListingExtension.BannerColor, fontSize: GameFont.Medium, anchor: TextAnchor.MiddleCenter, rowGap: RowHeight);
							}
						}
						if (info.subCategory != currentSubCategory)
						{
							currentSubCategory = info.subCategory;
							if (!currentSubCategory.NullOrEmpty())
							{
								listing.Header(currentSubCategory, ListingExtension.BannerColor, fontSize: GameFont.Small, anchor: TextAnchor.MiddleCenter, rowGap: RowHeight);
							}
						}
						if (DrawField(info, out bool fieldChange) && fieldChange)
						{
							FieldChanged();
						}
					}
				}
				listing.EndScrollView(ref viewRect);
			}
			GUIState.Pop();
		}

		/// <summary>
		/// Draw field for editing in the TweakValues menu
		/// </summary>
		/// <param name="info"></param>
		/// <returns>True if value was changed this frame.</returns>
		private bool DrawField(TweakInfo info, out bool fieldChanged)
		{
			fieldChanged = false;
			UISettingsType settingsType = info.settingsType;
			switch (info.settingsType)
			{
				case UISettingsType.None:
					return false;
				case UISettingsType.Checkbox:
					if (info.TryGetValue(out bool checkOn))
					{
						bool checkAfter = checkOn;
						listing.CheckboxLabeled(info.Name, ref checkAfter, string.Empty, string.Empty, false, lineHeight: RowHeight);
						if (checkOn != checkAfter)
						{
							info.SetValue(checkAfter);
							fieldChanged = true;
						}
						return true;
					}
					return false;
				case UISettingsType.IntegerBox:
					{
						if (info.TryGetValue(out int value))
						{
							int newValue = value;
							if (info.fieldInfo.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
							{
								listing.IntegerBox(info.Name, ref newValue, string.Empty, string.Empty, min: Mathf.RoundToInt(inputBox.MinValue), max: Mathf.RoundToInt(inputBox.MaxValue), lineHeight: RowHeight);
							}
							else
							{
								listing.IntegerBox(info.Name, ref newValue, string.Empty, string.Empty, min: 0, max: int.MaxValue, lineHeight: RowHeight);
							}
							if (value != newValue)
							{
								info.SetValue(newValue);
								fieldChanged = true;
							}
							return true;
						}
						return false;
					}
				case UISettingsType.FloatBox:
					{
						if (info.TryGetValue(out Vector2 vector2))
						{
							Vector2 newValue = vector2;
							listing.Vector2Box(info.Name, ref newValue, labelProportion: VectorLabelProportion, subLabelProportions: VectorSubLabelProportion, buffer: 5);
							if (vector2 != newValue)
							{
								info.SetValue(newValue);
								fieldChanged = true;
							}
							return true;
						}
						else if (info.TryGetValue(out FloatRange floatRange))
						{
							FloatRange newValue = floatRange;
							listing.FloatRangeBox(info.Name, ref newValue, labelProportion: FloatRangeLabelProportion, subLabelProportions: FloatRangeSubLabelProportion, buffer: 5);
							if (floatRange != newValue)
							{
								info.SetValue(newValue);
								fieldChanged = true;
							}
							return true;
						}
						else if (info.TryGetValue(out Vector3 vector3))
						{
							Vector3 newValue = vector3;
							listing.Vector3Box(info.Name, ref newValue, labelProportion: VectorLabelProportion, subLabelProportions: VectorSubLabelProportion, buffer: 5);
							if (vector3 != newValue)
							{
								info.SetValue(newValue);
								fieldChanged = true;
							}
							return true;
						}
						else if (info.TryGetValue(out float @float))
						{
							float newValue = @float;
							if (info.fieldInfo.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
							{
								listing.FloatBox(info.Name, ref newValue, string.Empty, string.Empty, min: inputBox.MinValue, max: inputBox.MaxValue, lineHeight: RowHeight);
							}
							else
							{
								listing.FloatBox(info.Name, ref newValue, string.Empty, string.Empty, min: 0, max: float.MaxValue, lineHeight: RowHeight);
							}
							if (@float != newValue)
							{
								info.SetValue(newValue);
								fieldChanged = true;
							}
							return true;
						}
						return false;
					}
				case UISettingsType.ToggleLabel:

					Color color = Color.white;
					Color highlightedColor = new Color(0.1f, 0.85f, 0.85f);
					Color clickColor = new Color(highlightedColor.r - 0.15f, highlightedColor.g - 0.15f, highlightedColor.b - 0.15f);

					if (info.TryGetValue(out Rot4 rot4))
					{
						if (listing.ClickableLabel(info.Name, rot4.ToStringWord(), highlightedColor, color, clickColor: clickColor, lineHeight: RowHeight))
						{
							rot4.Rotate(RotationDirection.Clockwise);
							info.SetValue(rot4);
							fieldChanged = true;
						}
						return true;
					}
					else if (info.TryGetValue(out Rot8 rot8))
					{
						if (listing.ClickableLabel(info.Name, rot8.ToStringNamed(), highlightedColor, color, clickColor: clickColor, lineHeight: RowHeight))
						{
							rot8.Rotate(RotationDirection.Clockwise);
							info.SetValue(rot8);
							fieldChanged = true;
						}
						return true;
					}
					return false;
				case UISettingsType.SliderEnum:
					{
						if (info.TryGetValue(out int value))
						{
							int newValue = value;
							listing.EnumSliderLabeled(info.Name, ref newValue, string.Empty, string.Empty, info.fieldInfo.FieldType);
							if (value != newValue)
							{
								info.SetValue(newValue);
								fieldChanged = true;
							}
							return true;
						}
					}
					return false;
				case UISettingsType.SliderInt:
					{
						if (info.TryGetValue(out int value))
						{
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
								fieldChanged = true;
							}
							return true;
						}
					}
					return false;
				case UISettingsType.SliderFloat:
					{
						if (info.TryGetValue(out float value))
						{
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
								fieldChanged = true;
							}
							return true;
						}
					}
					return false;
				case UISettingsType.SliderPercent:
					{
						if (info.TryGetValue(out float value))
						{
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
								fieldChanged = true;
							}
							return true;
						}
					}
					return false;
				default:
					Log.ErrorOnce($"{settingsType} has not yet been implemented for PostToSettings.DrawLister. Please notify SmashPhil.", settingsType.ToString().GetHashCode());
					return false;
			}
		}

		private void FieldChanged()
		{
			foreach (TweakInfo info in tweakValueFields)
			{
				if (info.instance is ITweakFields tweakFields)
				{
					tweakFields.OnFieldChanged();
				}
			}
		}

		private struct TweakInfo
		{
			public FieldInfo fieldInfo;
			public object instance;

			public string category;
			public string subCategory;

			public UISettingsType settingsType;

			public object initialValue;

			public string Name
			{
				get
				{
					if (IndexInList >= 0)
					{
						return $"{fieldInfo.Name}_{IndexInList + 1}"; //Xml indices are 1-based
					}
					return fieldInfo.Name;
				}
			}

			public int IndexInList { get; internal set; }

			public bool TryGetValue<T>(out T value) where T : struct
			{
				value = default;
				if (typeof(T) != fieldInfo.FieldType && typeof(T) != Nullable.GetUnderlyingType(fieldInfo.FieldType))
				{
					if (!fieldInfo.FieldType.IsEnum || typeof(T) != typeof(int))
					{
						return false;
					}
				}
				object rawValue = fieldInfo.GetValue(instance);
				if (Nullable.GetUnderlyingType(fieldInfo.FieldType) == typeof(T))
				{
					T? nullableValue = (T?)rawValue;
					if (!nullableValue.HasValue)
					{
						return false;
					}
					rawValue = nullableValue.Value;
				}
				if (rawValue.GetType() != typeof(T) && (!fieldInfo.FieldType.IsEnum || typeof(T) != typeof(int)))
				{
					Log.Error($"Invalid Cast: {rawValue.GetType()} to {typeof(T)}");
					return false;
				}
				value = (T)rawValue;
				return true;
			}

			public void SetValue<T>(T value)
			{
				fieldInfo.SetValue(instance, value);
			}
		}
	}
}
