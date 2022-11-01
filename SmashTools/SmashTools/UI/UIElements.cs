using System;
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

		public static readonly Color InactiveColor = new Color(0.37f, 0.37f, 0.37f, 0.8f);

		private static readonly Texture2D RadioButOffTex = ContentFinder<Texture2D>.Get("UI/Widgets/RadioButOff", true);

		public static string ToHex(this Color c) => $"#{ColorUtility.ToHtmlStringRGB(c)}";

		public static void Header(Rect rect, string header, Color highlight, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
		{
			GUIState.Push();
			{
				GUI.color = highlight;
				GUI.DrawTexture(rect, BaseContent.WhiteTex);

				GUIState.Reset();

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

		public static bool CheckboxLabeled(Rect rect, string label, ref bool checkOn, bool disabled = false, Texture2D texChecked = null, Texture2D texUnchecked = null, bool placeCheckboxNearText = false)
		{
			bool clicked = false;
			GUIState.Push();
			Text.Anchor = TextAnchor.MiddleLeft;
			if (placeCheckboxNearText)
			{
				rect.width = Mathf.Min(rect.width, Text.CalcSize(label).x + 24f + 10f);
			}
			Widgets.Label(rect, label);
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
			GUIState.Pop();
			return clicked;
		}

		public static bool ReverseRadioButton(Rect rect, string label, bool enabled)
		{
			GUIState.Push();
			Text.Anchor = TextAnchor.MiddleLeft;
			bool flag = Widgets.ButtonInvisible(rect, true);
			if (flag && !enabled)
			{
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			}
			Rect labelRect = new Rect(rect.x + 28f, rect.y, rect.width - 24, rect.height);
			Widgets.Label(labelRect, label);
			RadioButtonDraw(rect.x, rect.y + rect.height / 2f - 12f, enabled);
			GUIState.Pop();
			return flag;
		}

		public static void RadioButtonDraw(float x, float y, bool chosen)
		{
			GUIState.Push();
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
			GUIState.Pop();
		}

		public static void Vector2Box(Rect rect, string label, ref Vector2 value, string tooltip = null, float labelProportion = 0.45f, float buffer = 0)
		{
			value = Vector2Box(rect, label, value, tooltip, labelProportion, buffer);
		}

		public static Vector2 Vector2Box(Rect rect, string label, Vector2 value, string tooltip = null, float labelProportion = 0.45f, float buffer = 0)
		{
			GUIState.Push();
			float x = value.x;
			float y = value.y;

			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}

			Rect labelRect = new Rect(rect.x, rect.y, rect.width / 3, rect.height);
			if (!label.NullOrEmpty())
			{
				Widgets.Label(labelRect, label);
			}

			Rect inputRect = new Rect(rect.x + labelRect.width, rect.y, rect.width * 2 / 3, rect.height);
			Rect[] rects = inputRect.Split(2, buffer);

			NumericBox(rects[0], ref x, "x", string.Empty, string.Empty, float.MinValue, float.MaxValue, labelProportion);
			NumericBox(rects[1], ref y, "y", string.Empty, string.Empty, float.MinValue, float.MaxValue, labelProportion);
			value.x = x;
			value.y = y;
			GUIState.Pop();

			return value;
		}

		public static void Vector3Box(Rect rect, string label, ref Vector3 value, string tooltip = null, float labelProportion = 0.45f, float buffer = 0)
		{
			value = Vector3Box(rect, label, value, tooltip, labelProportion, buffer);
		}

		public static Vector3 Vector3Box(Rect rect, string label, Vector3 value, string tooltip = null, float labelProportion = 0.45f, float buffer = 0)
		{
			GUIState.Push();
			float x = value.x;
			float y = value.y;
			float z = value.z;
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}

			Rect labelRect = new Rect(rect.x, rect.y, rect.width / 3, rect.height);
			Widgets.Label(labelRect, label);

			Rect inputRect = new Rect(rect.x + labelRect.width, rect.y, rect.width * 2 / 3, rect.height);
			Rect[] rects = inputRect.Split(3, buffer);

			NumericBox(rects[0], ref x, "x", string.Empty, string.Empty, float.MinValue, float.MaxValue, labelProportion);
			NumericBox(rects[1], ref y, "y", string.Empty, string.Empty, float.MinValue, float.MaxValue, labelProportion);
			NumericBox(rects[2], ref z, "z", string.Empty, string.Empty, float.MinValue, float.MaxValue, labelProportion);
			value.x = x;
			value.y = y;
			value.z = z;

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
			Text.Font = fontSize;
			GUI.color = highlight;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = textColor;

			Widgets.Label(rect, label);
			GUIState.Pop();
		}

		public static bool ClickableLabel(Rect rect, string label, Color mouseOver, Color textColor, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft, Color? clickColor = null)
		{
			GUIState.Push();
			Text.Font = fontSize;
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

			GUIState.Pop();

			return Widgets.ButtonInvisible(rect);
		}

		public static void SliderLabeled(Rect rect, string label, string tooltip, string endSymbol, ref float value, float min, float max, float multiplier = 1f, int decimalPlaces = 2, float endValue = -1f, string maxValueDisplay = "")
		{
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
				TooltipHandler.TipRegion(rect, tooltip);
			}
			value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
			if (endValue > 0 && value >= max)
			{
				value = endValue;
			}
		}

		public static void DrawLineHorizontalGrey(float x, float y, float length)
		{
			GUI.DrawTexture(new Rect(x, y, length, 1f), BaseContent.GreyTex);
		}

		public static void DrawLineVerticalGrey(float x, float y, float length)
		{
			GUI.DrawTexture(new Rect(x, y, 1f, length), BaseContent.GreyTex);
		}

		public static void LabelStyled(Rect rect, string label, GUIStyle style)
		{
			Rect position = rect;
			float num = Prefs.UIScale / 2f;
			if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > 1E-45f)
			{
				position.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
				position.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
				position.xMax = Widgets.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
				position.yMax = Widgets.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
			}
			GUI.Label(position, label, style);
		}

		public static void LabelOutlineStyled(Rect rect, string label, GUIStyle style, Color outerColor)
		{
			Rect position = rect;
			float num = Prefs.UIScale / 2f;
			if (Prefs.UIScale > 1f && Math.Abs(num - Mathf.Floor(num)) > 1E-45f)
			{
				position.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
				position.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
				position.xMax = Widgets.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
				position.yMax = Widgets.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
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
			bool rotate = angle != 0 && angle != 360;
			Matrix4x4 matrix = GUI.matrix;
			try
			{
				if (rotate)
				{
					matrix = GUI.matrix;
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
			if (doBorder)
			{
				GUI.DrawTexture(rect, bgTex);
				rect = rect.ContractedBy(3f);
			}
			if (bgTex != null)
			{
				GUI.DrawTexture(rect, bgTex);
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

			if(bgFillPercent != 0)
			{
				if(bgFillPercent < 0)
				{
					Rect rectBG = rect;

					if(fillPercent + bgFillPercent < 0)
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

		/// <summary>
		/// Convert RenderTexture to Texture2D
		/// <para>Warning: This is very costly. Do not do often.</para>
		/// </summary>
		/// <param name="rTex"></param>
		public static Texture2D ConvertToTexture2D(this RenderTexture rTex)
		{
			Texture2D tex2d = new Texture2D(512, 512, TextureFormat.RGB24, false);
			RenderTexture.active = rTex;
			tex2d.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
			tex2d.Apply();
			return tex2d;
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
