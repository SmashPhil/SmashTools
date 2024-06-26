﻿using System;
using System.Text.RegularExpressions;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.Sound;
using RimWorld;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class UIElements
	{
		public static readonly Regex ValidInputRegex = new Regex(@"^(\#[A-Fa-f0-9]{0,7}$)");

		public static readonly Texture2D RadioButOffTex = ContentFinder<Texture2D>.Get("UI/Widgets/RadioButOff", true);
		public static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");

		public static readonly Color InactiveColor = new Color(0.37f, 0.37f, 0.37f, 0.8f);
		public static readonly Color MenuSectionBGBorderColor = new ColorInt(135, 135, 135).ToColor;
		public static readonly Color RangeControlTextColor = new Color(0.6f, 0.6f, 0.6f);

		private static int sliderDraggingID;

		private static float lastDragSliderSoundTime = -1f;

		public static string ToHex(this Color c) => $"#{ColorUtility.ToHtmlStringRGB(c)}";

		public static void Header(Rect rect, string header, Color highlight, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
		{
			GUIState.Push();
			{
				GUI.color = highlight;
				GUI.DrawTexture(rect, BaseContent.WhiteTex);

				GUIState.Reset();

				Text.Font = fontSize;
				Text.Anchor = anchor;
				Widgets.Label(rect, header);
			}
			GUIState.Pop();
		}

		public static void CheckboxDraw(float x, float y, bool active, bool disabled, float size = 24f, Texture2D texChecked = null, Texture2D texUnchecked = null)
		{
			Color color = GUI.color;
			if (disabled)
			{
				GUI.color = InactiveColor;
			}
			Texture2D image;
			if (active)
			{
				image = ((texChecked != null) ? texChecked : Widgets.CheckboxOnTex);
			}
			else
			{
				image = ((texUnchecked != null) ? texUnchecked : Widgets.CheckboxOffTex);
			}
			GUI.DrawTexture(new Rect(x, y, size, size), image);
			if (disabled)
			{
				GUI.color = color;
			}
		}

		public static bool CheckboxLabeled(Rect rect, string label, bool checkOn, bool disabled = false, Texture2D texChecked = null, Texture2D texUnchecked = null, TextAnchor labelAnchor = TextAnchor.MiddleLeft)
		{
			bool value = checkOn;
			CheckboxLabeled(rect, label, ref value, disabled: disabled, texChecked: texChecked, texUnchecked: texUnchecked, labelAnchor: labelAnchor);
			return value;
		}

		public static bool CheckboxLabeled(Rect rect, string label, ref bool checkOn, bool disabled = false, Texture2D texChecked = null, Texture2D texUnchecked = null, TextAnchor labelAnchor = TextAnchor.MiddleLeft)
		{
			bool clicked = false;
			GUIState.Push();
			{
				Text.Anchor = labelAnchor;
				Rect labelRect = new Rect(rect)
				{
					width = rect.width - 24
				};
				Widgets.Label(labelRect, label);
				if (!disabled && Widgets.ButtonInvisible(rect, true))
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
				CheckboxDraw(rect.x + rect.width - 24f, rect.y, checkOn, disabled, 20, null, null);
			}
			GUIState.Pop();
			return clicked;
		}

		public static bool ReverseRadioButton(Rect rect, string label, bool enabled)
		{
			bool flag;
			GUIState.Push();
			{
				Text.Anchor = TextAnchor.MiddleLeft;
				flag = Widgets.ButtonInvisible(rect, true);
				if (flag && !enabled)
				{
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
				}
				Rect labelRect = new Rect(rect.x + 28f, rect.y, rect.width - 24, rect.height);
				Widgets.Label(labelRect, label);
				RadioButtonDraw(rect.x, rect.y + rect.height / 2f - 12f, enabled);
			}
			GUIState.Pop();
			return flag;
		}

		public static void RadioButtonDraw(float x, float y, bool chosen)
		{
			GUIState.Push();
			{
				GUI.color = Color.white;
				Texture2D image;
				if (chosen)
				{
					image = Widgets.RadioButOnTex;
				}
				else
				{
					image = RadioButOffTex;
				}
				GUI.DrawTexture(new Rect(x, y, 24f, 24f), image);
			}
			GUIState.Pop();
		}

		public static void Vector2Box(Rect rect, string label, ref Vector2 value, string tooltip = null, float labelProportion = 0.45f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			value = Vector2Box(rect, label, value, tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
		}

		public static Vector2 Vector2Box(Rect rect, string label, Vector2 value, string tooltip = null, float labelProportion = 0.45f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			(float x, float y) = SplitFloatBoxes(rect, label, (value.x, "x"), (value.y, "y"), tooltip: tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
			return new Vector2(x, y);
		}

		public static void FloatRangeBox(Rect rect, string label, ref FloatRange value, string tooltip = null, float labelProportion = 0.45f, float subLabelProportions = 0.25f, float buffer = 0)
		{
			value = FloatRangeBox(rect, label, value, tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
		}

		public static FloatRange FloatRangeBox(Rect rect, string label, FloatRange value, string tooltip = null, float labelProportion = 0.45f, float subLabelProportions = 0.25f, float buffer = 0)
		{
			(float x, float y) = SplitFloatBoxes(rect, label, (value.min, "min"), (value.max, "max"), tooltip: tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
			return new FloatRange(x, y);
		}

		private static (float left, float right) SplitFloatBoxes(Rect rect, string label, (float value, string label) leftBox, (float value, string label) rightBox, string tooltip = null, float labelProportion = 0.45f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			float x = leftBox.value;
			float y = rightBox.value;

			GUIState.Push();
			{
				if (!tooltip.NullOrEmpty())
				{
					TooltipHandler.TipRegion(rect, tooltip);
				}

				Rect labelRect = new Rect(rect.x, rect.y, rect.width * labelProportion, rect.height);
				if (!label.NullOrEmpty())
				{
					Widgets.Label(labelRect, label);
				}

				Rect inputRect = new Rect(labelRect.xMax, rect.y, rect.width - labelRect.width, rect.height);
				Rect[] rects = inputRect.Split(2, buffer);

				NumericBox(rects[0], ref x, leftBox.label, string.Empty, string.Empty, float.MinValue, float.MaxValue, subLabelProportions);
				NumericBox(rects[1], ref y, rightBox.label, string.Empty, string.Empty, float.MinValue, float.MaxValue, subLabelProportions);
			}
			GUIState.Pop();
			return (x, y);
		}

		public static void Vector3Box(Rect rect, string label, ref Vector3 value, string tooltip = null, float labelProportion = 0.45f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			value = Vector3Box(rect, label, value, tooltip, labelProportion, subLabelProportions, buffer);
		}

		public static Vector3 Vector3Box(Rect rect, string label, Vector3 value, string tooltip = null, float labelProportion = 0.45f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			GUIState.Push();
			{
				float x = value.x;
				float y = value.y;
				float z = value.z;
				if (!tooltip.NullOrEmpty())
				{
					TooltipHandler.TipRegion(rect, tooltip);
				}

				Rect labelRect = new Rect(rect.x, rect.y, rect.width * labelProportion, rect.height);
				Widgets.Label(labelRect, label);

				Rect inputRect = new Rect(rect.x + labelRect.width, rect.y, rect.width - labelRect.width, rect.height);
				Rect[] rects = inputRect.Split(3, buffer);

				NumericBox(rects[0], ref x, "x", string.Empty, string.Empty, float.MinValue, float.MaxValue, subLabelProportions);
				NumericBox(rects[1], ref y, "y", string.Empty, string.Empty, float.MinValue, float.MaxValue, subLabelProportions);
				NumericBox(rects[2], ref z, "z", string.Empty, string.Empty, float.MinValue, float.MaxValue, subLabelProportions);
				value.x = x;
				value.y = y;
				value.z = z;
			}
			GUIState.Pop();
			return value;
		}

		public static void NumericBox<T>(Rect rect, ref T value, string label, string tooltip, string disabledTooltip, float min = int.MinValue, float max = int.MaxValue, float labelProportion = 0.45f) where T : struct
		{
			value = NumericBox(rect, value, label, tooltip, disabledTooltip, min: min, max: max, labelProportion: labelProportion);
		}

		public static T NumericBox<T>(Rect rect, T value, string label, string tooltip, string disabledTooltip, float min = int.MinValue, float max = int.MaxValue, float labelProportion = 0.45f) where T : struct
		{
			GUIState.Push();
			{
				float proportion = Mathf.Clamp01(labelProportion);
				bool disabled = !disabledTooltip.NullOrEmpty();
				float centerY = rect.y + (rect.height - Text.LineHeight) / 2;
				float leftLength = rect.width * proportion;
				float rightLength = rect.width * (1 - proportion);
				Rect rectLeft = new Rect(rect.x, centerY, leftLength, rect.height);
				Rect rectRight = new Rect(rect.x + rect.width - rightLength, centerY, rightLength, Text.LineHeight);

				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					GUIState.Disable();
					TooltipHandler.TipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					TooltipHandler.TipRegion(rect, tooltip);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						//Event.current.Use();
						//List<FloatMenuOption> options = new List<FloatMenuOption>();
						//options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						//{
						//	ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
						//	VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						//}));
						//FloatMenu floatMenu = new FloatMenu(options)
						//{
						//	vanishIfMouseDistant = true
						//};
						//Find.WindowStack.Add(floatMenu);
					}
				}
				Widgets.Label(rectLeft, label);

				Text.CurTextFieldStyle.alignment = TextAnchor.MiddleRight;
				string buffer = value.ToString();

				Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);
			}
			GUIState.Pop();

			return value;
		}

		public static string HexField(string label, Rect rect, string text)
		{
			Widgets.Label(rect.LeftPart(0.3f), label);
			Rect rect2 = rect.RightPart(0.7f);
			return Widgets.TextField(rect2, '#' + text, 7, ValidInputRegex).Replace("#", "");
		}

		public static void DrawLabel(Rect rect, string label, Color highlight, Color textColor, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
		{
			GUIState.Push();
			{
				Text.Font = fontSize;
				GUI.color = highlight;
				GUI.DrawTexture(rect, BaseContent.WhiteTex);
				GUI.color = textColor;

				Text.Anchor = anchor;
				Widgets.Label(rect, label);
			}
			GUIState.Pop();
		}

		public static bool ClickableLabel(Rect rect, string label, Color mouseOver, Color textColor, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft, Color? clickColor = null)
		{
			GUIState.Push();
			{
				Text.Font = fontSize;
				Text.Anchor = anchor;
				if (Mouse.IsOver(rect))
				{
					GUI.color = mouseOver;
					if (Input.GetMouseButton(0))
					{
						clickColor ??= Color.grey;
						GUI.color = clickColor.Value;
					}
				}
				else
				{
					GUI.color = textColor;
				}
				Widgets.Label(rect, label);
			}
			GUIState.Pop();

			if (Widgets.ButtonInvisible(rect))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				return true;
			}
			return false;
		}

		public static void SliderLabeled(Rect rect, string label, string tooltip, string endSymbol, ref float value, float min, float max, float multiplier = 1f, int decimalPlaces = 2, float endValue = -1f, string maxValueDisplay = "")
		{
			Rect fullRect = rect;
			rect.y += rect.height / 2;
			rect.height /= 2;
			string format = $"{Math.Round(value * multiplier, decimalPlaces)}" + endSymbol;
			if (!maxValueDisplay.NullOrEmpty() && endValue > 0)
			{
				if (value >= endValue)
				{
					format = maxValueDisplay;
				}
			}
			if (Mouse.IsOver(fullRect))
			{
				Widgets.DrawHighlight(fullRect);
			}
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(fullRect, tooltip);
			}
			value = Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
			if (endValue > 0 && value >= max)
			{
				value = endValue;
			}
		}

		public static void DrawLineHorizontal(float x, float y, float length, Color color)
		{
			GUIState.Push();
			{
				GUI.color = color;
				GUI.DrawTexture(new Rect(x, y, length, 1f), BaseContent.WhiteTex);
			}
			GUIState.Pop();
		}

		public static void DrawLineVertical(float x, float y, float length, Color color)
		{
			GUIState.Push();
			{
				GUI.color = color;
				GUI.DrawTexture(new Rect(x, y, 1f, length), BaseContent.WhiteTex);
			}
			GUIState.Pop();
		}

		public static void DrawLineHorizontalGrey(float x, float y, float length)
		{
			GUI.DrawTexture(new Rect(x, y, length, 1f), BaseContent.GreyTex);
		}

		public static void DrawLineVerticalGrey(float x, float y, float length)
		{
			GUI.DrawTexture(new Rect(x, y, 1f, length), BaseContent.GreyTex);
		}

		public static float HorizontalSlider_Arrow(Rect rect, float value, float min, float max, float roundTo = 0, float handleScale = 20, Texture2D railAtlas = null)
		{
			int screenPointHashCode = UI.GUIToScreenPoint(new Vector2(rect.x, rect.y)).GetHashCode();
			screenPointHashCode = Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(screenPointHashCode, max), min), rect.height), rect.width);

			float valueChange = value;

			GUIState.Push();
			{
				Rect sliderRect = new Rect(rect);
				sliderRect.xMin += 6f;
				sliderRect.xMax -= 6f;

				Rect atlasRect = new Rect(sliderRect.x, sliderRect.y + 2f, sliderRect.width, 8f);
				if (railAtlas)
				{
					GUI.color = RangeControlTextColor;
					Widgets.DrawAtlas(atlasRect, railAtlas);
				}

				GUI.color = Color.white;

				float x = Mathf.Clamp(sliderRect.x - 6f + sliderRect.width * Mathf.InverseLerp(min, max, valueChange), sliderRect.xMin - 6f, sliderRect.xMax - 6f);
				GUI.DrawTexture(new Rect(x, atlasRect.center.y - 6f, handleScale, handleScale), TargetLevelArrow);

				if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect) && sliderDraggingID != screenPointHashCode)
				{
					sliderDraggingID = screenPointHashCode;
					SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
					Event.current.Use();
				}
				if (sliderDraggingID == screenPointHashCode && UnityGUIBugsFixer.MouseDrag(0))
				{
					valueChange = Mathf.Clamp((Event.current.mousePosition.x - sliderRect.x) / sliderRect.width * (max - min) + min, min, max);
					if (Event.current.type == EventType.MouseDrag)
					{
						Event.current.Use();
					}
				}
				if (roundTo > 0f)
				{
					valueChange = valueChange.RoundTo(roundTo);
				}
				if (value != valueChange)
				{
					CheckPlayDragSliderSound();
				}
			}
			GUIState.Pop();

			return valueChange;
		}

		/// <summary>
		/// Copied from Widgets.CheckPlayDragSliderSound since it is private
		/// </summary>
		private static void CheckPlayDragSliderSound()
		{
			if (Time.realtimeSinceStartup > lastDragSliderSoundTime + 0.075f)
			{
				SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
				lastDragSliderSoundTime = Time.realtimeSinceStartup;
			}
		}

		public static void LabelStyled(Rect rect, string label, GUIStyle style)
		{
			Rect position = rect;
			float num = Prefs.UIScale / 2f;
			if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > 1E-45f)
			{
				//position.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
				//position.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
				//position.xMax = Widgets.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
				//position.yMax = Widgets.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
			}
			GUI.Label(position, label, style);
		}

		public static void LabelOutlineStyled(Rect rect, string label, GUIStyle style, Color outerColor)
		{
			Rect position = rect;
			float num = Prefs.UIScale / 2f;
			if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > 1E-45f)
			{
				//position.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
				//position.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
				//position.xMax = Widgets.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
				//position.yMax = Widgets.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
			}

			var innerColor = style.normal.textColor;
			style.normal.textColor = outerColor;
			position.x--;
			GUI.Label(position, label, style);
			position.x += 2;
			GUI.Label(position, label, style);
			position.x--;
			position.y--;
			GUI.Label(position, label, style);
			position.y += 2;
			GUI.Label(position, label, style);
			position.y--;
			style.normal.textColor = innerColor;
			GUI.Label(position, label, style);
		}

		/// <summary>
		/// Draw <paramref name="texture"/> with <paramref name="material"/> rotated by <paramref name="angle"/>
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="texture"></param>
		/// <param name="material"></param>
		/// <param name="angle"></param>
		/// <param name="texCoords"></param>
		public static void DrawTextureWithMaterialOnGUI(Rect rect, Texture texture, Material material, float angle, Rect texCoords = default)
		{
			Matrix4x4 matrix = GUI.matrix;
			try
			{
				if (angle != 0 && angle != 360)
				{
					UI.RotateAroundPivot(angle, rect.center);
				}
				GenUI.DrawTextureWithMaterial(rect, texture, material, texCoords);
			}
			finally
			{
				GUI.matrix = matrix;
			}
		}

		/// <summary>
		/// Draw vertical fillable bar
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="fillPercent"></param>
		/// <param name="flip"></param>
		public static Rect VerticalFillableBar(Rect rect, float fillPercent, bool flip = false)
		{
			return VerticalFillableBar(rect, fillPercent, UIData.FillableBarTexture, flip);
		}

		/// <summary>
		/// Draw vertical fillable bar with texture
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="fillPercent"></param>
		/// <param name="fillTex"></param>
		/// <param name="flip"></param>
		public static Rect VerticalFillableBar(Rect rect, float fillPercent, Texture2D fillTex, bool flip = false)
		{
			bool doBorder = rect.height > 15f && rect.width > 20f;
			return VerticalFillableBar(rect, fillPercent, fillTex, UIData.ClearBarTexture, doBorder, flip);
		}

		/// <summary>
		/// Draw vertical fillable bar with background texture
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="fillPercent"></param>
		/// <param name="fillTex"></param>
		/// <param name="bgTex"></param>
		/// <param name="doBorder"></param>
		/// <param name="flip"></param>
		public static Rect VerticalFillableBar(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex, bool doBorder = false, bool flip = false)
		{
			if (bgTex != null)
			{
				GUI.DrawTexture(rect, bgTex);
				if (doBorder)
				{
					rect = rect.ContractedBy(3f);
				}
			}
			if (!flip)
			{
				rect.y += rect.height;
				rect.height *= -1;
			}
			Rect result = rect;
			rect.height *= fillPercent;
			GUI.DrawTexture(rect, fillTex);
			return result;
		}

		public static void FillableBar(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex, float innerContractedBy = 0)
		{
			GUI.DrawTexture(rect, bgTex);
			rect = rect.ContractedBy(innerContractedBy);

			Rect fullBarRect = rect;
			fullBarRect.width *= fillPercent;
			GUI.DrawTexture(fullBarRect, fillTex);
		}

		public static void FillableBarLabeled(Rect rect, float fillPercent, string label, Texture2D fillTex, Texture2D addedFillTex, Texture2D innerTex, Texture2D outlineTex, float? actualValue = null, float addedValue = 0f, float bgFillPercent = 0f)
		{
			if (fillPercent < 0f)
			{
				fillPercent = 0f;
			}
			if (fillPercent > 1f)
			{
				fillPercent = 1f;
			}

			if (fillTex != null && addedFillTex != null)
			{
				FillableBarHollowed(rect, fillPercent, bgFillPercent, fillTex, addedFillTex, innerTex, outlineTex);
			
				Rect rectLabel = rect;
				rectLabel.x += 5f;

				GUIStyle style = new GUIStyle(Text.CurFontStyle);
				//style.fontStyle = FontStyle.Bold;

				LabelOutlineStyled(rectLabel, label, style, Color.black);
				if(actualValue != null)
				{
					Rect valueRect = rect;
					valueRect.width /= 2;
					valueRect.x = rectLabel.x + rectLabel.width / 2 - 6f;
					style.alignment = TextAnchor.MiddleRight;

					string value = string.Format("{1} {0}", actualValue.ToString(), addedValue != 0 ? "(" + (addedValue > 0 ? "+" : "") +  addedValue.ToString() + ")" : "");
					LabelOutlineStyled(valueRect, value, style, Color.black);
					//GUI.DrawTexture(valueRect, fillTex); //For Alignment
				}
			}
		}

		public static void FillableBarHollowed(Rect rect, float fillPercent, float bgFillPercent, Texture2D fillTex, Texture2D addedFillTex, Texture2D innerTex, Texture2D bgTex)
		{
			GUI.DrawTexture(rect, bgTex);
			rect = rect.ContractedBy(2f);

			Rect rect2 = rect;
			rect2.width -= 2f;
			GUI.DrawTexture(rect2, innerTex);


			Rect fullBarRect = rect;
			fullBarRect.width *= fillPercent;
			GUI.DrawTexture(fullBarRect, fillTex);

			if (bgFillPercent != 0)
			{
				if (bgFillPercent < 0)
				{
					Rect rectBG = rect;

					if (fillPercent + bgFillPercent < 0)
					{
						rectBG.width *= fillPercent;
						rectBG.x = fullBarRect.x;
					}
					else
					{
						rectBG.width *= bgFillPercent;
						rectBG.x = fullBarRect.x + fullBarRect.width;
					}
					GUI.DrawTexture(rectBG, innerTex);
				}
				else
				{
					Rect rectBG = rect;
					rectBG.x = fullBarRect.x + fullBarRect.width;
					rectBG.width *= bgFillPercent;
					GUI.DrawTexture(rectBG, addedFillTex);
				}
			}
		}

		public static void DrawBarThreshold(Rect barRect, float threshPct, float curLevel)
		{
			var color = GUI.color;
			float num = (barRect.width > 60f) ? 2 : 1;
			Rect position = new Rect(barRect.x + barRect.width * threshPct - (num - 1f), barRect.y + barRect.height / 2f, num, barRect.height / 2f);
			Texture2D image;
			if (threshPct < curLevel)
			{
				image = BaseContent.BlackTex;
				GUI.color = new Color(1f, 1f, 1f, 0.9f);
			}
			else
			{
				image = BaseContent.GreyTex;
				GUI.color = new Color(1f, 1f, 1f, 0.5f);
			}
			GUI.DrawTexture(position, image);
			GUI.color = color;
		}

		public static void LabelUnderlined(Rect rect, string label, Color labelColor, Color lineColor)
		{
			Color color = GUI.color;
			rect.y += 3f;
			GUI.color = labelColor;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(rect, label);
			rect.y += 20f;
			GUI.color = lineColor;
			Widgets.DrawLineHorizontal(rect.x - 1, rect.y, rect.width - 1);
			rect.y += 2f;
			GUI.color = color;
		}

		public static void LabelUnderlined(Rect rect, string label, string label2, Color labelColor, Color label2Color, Color lineColor)
		{
			Color color = GUI.color;
			rect.y += 3f;
			GUI.color = labelColor;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(rect, label);
			GUI.color = label2Color;
			Rect rect2 = new Rect(rect);
			rect2.x += Text.CalcSize(label).x + 5f;
			Widgets.Label(rect2, label2);
			rect.y += 20f;
			GUI.color = lineColor;
			Widgets.DrawLineHorizontal(rect.x - 1, rect.y, rect.width - 1);
			rect.y += 2f;
			GUI.color = color;
		}

		public static bool InfoCardButton(Rect rect)
		{
			MouseoverSounds.DoRegion(rect);
			TooltipHandler.TipRegionByKey(rect, "DefInfoTip");
			bool result = Widgets.ButtonImage(rect, TexButton.Info, GUI.color, true);
			UIHighlighter.HighlightOpportunity(rect, "InfoCard");
			return result;
		}
	}
}
