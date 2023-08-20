using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class ListingExtension
	{
		public static readonly Color HighlightColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

		public static readonly Color BannerColor = new Color(0f, 0f, 0f, 0.25f);

		public static readonly Color MenuSectionBGFillColor = new ColorInt(42, 43, 44).ToColor;

		public static readonly Color LightHighlightColor = new Color(1f, 1f, 1f, 0.04f);

		public static readonly Texture2D ButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG", true);

		public static readonly Texture2D ButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover", true);

		public static readonly Texture2D ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick", true);

		public static readonly Texture2D LightHighlight = SolidColorMaterials.NewSolidColorTexture(LightHighlightColor);

		public static void CheckboxLabeledWithMessage(this Listing_Standard lister, string label, Func<bool, Message> messageGetter, ref bool checkOn, string tooltip = null)
		{
			bool before = checkOn;
			lister.CheckboxLabeled(label, ref checkOn, tooltip);
			if (checkOn != before)
			{
				Message message = messageGetter(checkOn);
				if (message != null)
				{
					Messages.Message(message, false);
				}
			}
		}

		public static bool ReverseRadioButton(this Listing_Standard lister, string label, bool enabled, string tooltip = null, float? tooltipDelay = null)
		{
			float lineHeight = Text.LineHeight;
			Rect rect = lister.GetRect(lineHeight, 1f);
			if (lister.BoundingRectCached != null && !rect.Overlaps(lister.BoundingRectCached.Value))
			{
				return false;
			}
			if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
				TipSignal tip = tooltipDelay != null ? new TipSignal(tooltip, tooltipDelay.Value) : new TipSignal(tooltip);
				TooltipHandler.TipRegion(rect, tip);
			}
			bool result = UIElements.ReverseRadioButton(rect, label, enabled);
			lister.Gap(lister.verticalSpacing);
			return result;
		}

		public static void IntegerBox(this Listing lister, string text, string tooltip, ref int value, float labelLength, float padding, int min = int.MinValue, int max = int.MaxValue)
		{
			Rect rect = lister.GetRect(Text.LineHeight);

			Rect rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
			Rect rectRight = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding, rect.height);

			Widgets.Label(rectLeft, text);

			GUIState.Push();
			{
				Text.CurTextFieldStyle.alignment = TextAnchor.MiddleLeft;
				string buffer = value.ToString();
				Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

				if (!tooltip.NullOrEmpty())
				{
					if (Mouse.IsOver(rect))
					{
						Widgets.DrawHighlight(rect);
					}
					TooltipHandler.TipRegion(rect, tooltip);
				}
			}
			GUIState.Pop();
		}

		public static void Numericbox(this Listing lister, string text, string tooltip, ref float value, float labelLength, float padding, float min = -1E+09f, float max = 1E+09f)
		{
			lister.Gap(12f);
			Rect rect = lister.GetRect(Text.LineHeight);

			Rect rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
			Rect rectRight = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding, rect.height);

			Widgets.Label(rectLeft, text);

			GUIState.Push();
			{
				Text.CurTextFieldStyle.alignment = TextAnchor.MiddleLeft;
				string buffer = value.ToString();
				Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

				if (!tooltip.NullOrEmpty())
				{
					if (Mouse.IsOver(rect))
					{
						Widgets.DrawHighlight(rect);
					}
					TooltipHandler.TipRegion(rect, tooltip);
				}
			}
			GUIState.Pop();
		}

		public static void EnumSliderLabeled(this Listing lister, string label, ref int value, string tooltip, string disabledTooltip, Type enumType, bool translate = false)
		{
			GUIState.Push();
			{
				try
				{
					int[] enumValues = Enum.GetValues(enumType).Cast<int>().ToArray();
					string[] enumNames = Enum.GetNames(enumType);
					int min = enumValues[0];
					int max = enumValues.Last();
					Rect rect = lister.GetRect(24f);
					Rect fullRect = rect;
					rect.y += rect.height / 2;
					string format = Enum.GetName(enumType, value);
					if (translate)
					{
						format = format.Translate();
					}
					if (!disabledTooltip.NullOrEmpty())
					{
						GUIState.Disable();
						TooltipHandler.TipRegion(fullRect, disabledTooltip);
					}
					else if (!tooltip.NullOrEmpty())
					{
						if (Mouse.IsOver(fullRect))
						{
							Widgets.DrawHighlight(fullRect);
						}
						TooltipHandler.TipRegion(fullRect, tooltip);
					}
					GUIState.Reset();
					value = (int)Widgets.HorizontalSlider_NewTemp(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
				}
				catch (Exception ex)
				{
					Log.Error($"Unable to convert to {enumType}. Exception={ex.Message}");
					return;
				}
			}
			GUIState.Pop();
		}

		public static float SliderLabeled(this Listing lister, string label, string tooltip, string endSymbol, float value, float min, float max, float multiplier = 1f, int decimalPlaces = 2, float endValue = -1f, string maxValueDisplay = "")
		{
			SliderLabeled(lister, label, tooltip, endSymbol, ref value, min, max, multiplier: multiplier, decimalPlaces: decimalPlaces, endValue: endValue, maxValueDisplay: maxValueDisplay);
			return value;
		}

		public static void SliderLabeled(this Listing lister, string label, string tooltip, string endSymbol, ref float value, float min, float max, float multiplier = 1f, int decimalPlaces = 2, float endValue = -1f, string maxValueDisplay = "")
		{
			lister.Gap(12f);
			Rect rect = lister.GetRect(24f);
			Rect fullRect = rect;
			rect.y += rect.height / 2;

			string format = $"{Math.Round(value * multiplier, decimalPlaces)}" + endSymbol;
			if (!maxValueDisplay.NullOrEmpty() && endValue > 0)
			{
				if (value >= endValue)
				{
					format = maxValueDisplay;
				}
			}
			if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(fullRect))
				{
					Widgets.DrawHighlight(fullRect);
				}
				TooltipHandler.TipRegion(fullRect, tooltip);
			}
			value = Widgets.HorizontalSlider_NewTemp(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
			if (endValue > 0 && value >= max)
			{
				value = endValue;
			}
		}

		public static void SliderLabeled(this Listing lister, string label, string tooltip, string endSymbol, ref int value, int min, int max, int roundTo = 1, string maxValueDisplay = "", string minValueDisplay = "")
		{
			lister.Gap(12f);
			Rect rect = lister.GetRect(24f);
			Rect fullRect = new Rect(rect);
			rect.y += rect.height / 2;

			string format = string.Format("{0}" + endSymbol, value);
			if (!maxValueDisplay.NullOrEmpty())
			{
				if (value == max)
				{
					format = maxValueDisplay;
				}
			}
			if (!minValueDisplay.NullOrEmpty())
			{
				if (value == min)
				{
					format = minValueDisplay;
				}
			}
			if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(fullRect))
				{
					Widgets.DrawHighlight(fullRect);
				}
				TooltipHandler.TipRegion(fullRect, tooltip);
			}
			value = (int)Widgets.HorizontalSlider_NewTemp(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format).RoundTo(roundTo);
		}

		public static void SliderPercentLabeled(this Listing listing, string label, string tooltip, string endSymbol, ref float value, float min, float max, int decimalPlaces = 2,
			float endValue = -1f, string endValueDisplay = "")
		{
			GUIState.Push();
			try
			{
				Rect rect = listing.GetRect(24f);
				Rect fullRect = rect;
				rect.y += rect.height / 2;
				string format = $"{Math.Round(value * 100, decimalPlaces)}" + endSymbol;

				if (!endValueDisplay.NullOrEmpty() && endValue > 0)
				{
					if (value >= endValue)
					{
						format = endValueDisplay;
					}
				}
				bool mouseOver = Mouse.IsOver(fullRect);
				
				if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(fullRect);
					}
					TooltipHandler.TipRegion(fullRect, tooltip);
				}

				value = Widgets.HorizontalSlider_NewTemp(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
				if (endValue > 0 && value >= max)
				{
					value = endValue;
				}
			}
			finally
			{
				GUIState.Pop();
			}
		}

		public static void Header(this Listing lister, string header, Color highlight, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft, float rowGap = 16)
		{
			Listing_SplitColumns splitLister = lister as Listing_SplitColumns;

			splitLister?.NextRow(rowGap);

			Rect rect = lister.GetRect(Text.CalcHeight(header, lister.ColumnWidth));
			UIElements.Header(rect, header, highlight, fontSize: fontSize, anchor: anchor);

			splitLister?.Gap(2);
		}

		public static void Vector2Box(this Listing lister, string label, ref Vector2 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
		{
			value = lister.Vector2Box(label, value, tooltip, labelProportion, subLabelProportions);
		}

		public static Vector2 Vector2Box(this Listing lister, string label, Vector2 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
		{
			GUIState.Push();
			{
				Rect rect = lister.GetRect(24);
				UIElements.Vector2Box(rect, label, value, tooltip, labelProportion, subLabelProportions);
			}
			GUIState.Pop();
			return value;
		}

		public static void Vector3Box(this Listing lister, string label, ref Vector3 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
		{
			value = lister.Vector3Box(label, value, tooltip, labelProportion, subLabelProportions);
		}

		public static Vector3 Vector3Box(this Listing lister, string label, Vector3 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
		{
			GUIState.Push();
			{
				Rect rect = lister.GetRect(24);
				UIElements.Vector3Box(rect, label, value, tooltip, labelProportion, subLabelProportions);
			}
			GUIState.Pop();
			return value;
		}

		public static bool ListItemSelectable(this Listing lister, string header, Color hoverColor, bool selected = false, bool active = true, string disabledTooltip = null)
		{
			Rect rect = lister.GetRect(20f);

			GUIState.Push();
			{
				if (selected)
				{
					Widgets.DrawBoxSolid(rect, HighlightColor);
				}

				GUI.color = Color.white;
				if (!active)
				{
					GUI.color = Color.grey;
				}
				else if (Mouse.IsOver(rect))
				{
					GUI.color = hoverColor;
				}
				if (!disabledTooltip.NullOrEmpty())
				{
					TooltipHandler.TipRegion(rect, disabledTooltip);
				}

				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(rect, header);
			}
			GUIState.Pop();

			if (active && Widgets.ButtonInvisible(rect, true))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				return true;
			}
			return false;
		}

		public static bool ListItemButton(this Listing lister, string header, string buttonLabel, Color highlightColor, float buttonWidth = 30f, bool background = true, bool active = true)
		{
			var anchor = Text.Anchor;
			Color color = GUI.color;
			Rect rect = lister.GetRect(20f);
			Rect buttonRect = new Rect(rect.width - buttonWidth, rect.y, buttonWidth, rect.height);

			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect, header);

			if (background)
			{
				Texture2D atlas = ButtonBGAtlas;
				if (Mouse.IsOver(buttonRect))
				{
					atlas = ButtonBGAtlasMouseover;
					if (Input.GetMouseButton(0))
					{
						atlas = ButtonBGAtlasClick;
					}
				}
				Widgets.DrawAtlas(buttonRect, atlas);
			}
			else
			{
				GUI.color = Color.white;
				if (Mouse.IsOver(buttonRect))
				{
					GUI.color = highlightColor;
				}
			}
			if (background)
			{
				Text.Anchor = TextAnchor.MiddleCenter;
			}
			else
			{
				Text.Anchor = TextAnchor.MiddleRight;
			}
			bool wordWrap = Text.WordWrap;
			if (buttonRect.height < Text.LineHeight * 2f)
			{
				Text.WordWrap = false;
			}

			Widgets.Label(buttonRect, buttonLabel);
			Text.Anchor = anchor;
			GUI.color = color;
			Text.WordWrap = wordWrap;
			lister.Gap(2f);
			return Widgets.ButtonInvisible(buttonRect, false);
		}

		public static bool CheckboxLabeledReturned(this Listing_Standard lister, string label, ref bool checkOn, string tooltip = null)
		{
			float lineHeight = Text.LineHeight;
			Rect rect = lister.GetRect(lineHeight);
			if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
				TooltipHandler.TipRegion(rect, tooltip);
			}
			
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect, label);
			bool clicked = false;
			if (Widgets.ButtonInvisible(rect, true))
			{
				checkOn = !checkOn;
				clicked = true;
				if (checkOn)
				{
					SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
				}
				else
				{
					SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
				}
			}
			Texture2D image = checkOn ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
			GUI.DrawTexture(new Rect(rect.x + rect.width - 24f, rect.y, 24, 24), image);
			Text.Anchor = anchor;

			lister.Gap(lister.verticalSpacing);
			return clicked;
		}
	}
}
