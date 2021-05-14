using System;
using System.Text.RegularExpressions;
using UnityEngine;
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

		public static string ToHex(this Color c) => $"#{ColorUtility.ToHtmlStringRGB(c)}";

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

		public static void CheckboxLabeled(Rect rect, string label, ref bool checkOn, bool disabled = false, Texture2D texChecked = null, Texture2D texUnchecked = null, bool placeCheckboxNearText = false)
		{
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			if (placeCheckboxNearText)
			{
				rect.width = Mathf.Min(rect.width, Text.CalcSize(label).x + 24f + 10f);
			}
			Widgets.Label(rect, label);
			if (!disabled && Widgets.ButtonInvisible(rect, true))
			{
				checkOn = !checkOn;
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
			Text.Anchor = anchor;
		}

		public static string HexField(string label, Rect rect, string text)
		{
			Widgets.Label(rect.LeftPart(0.3f), label);
			Rect rect2 = rect.RightPart(0.7f);
			return Widgets.TextField(rect2, '#' + text, 7, ValidInputRegex).Replace("#", "");
		}

		public static void DrawLabel(Rect rect, string label, Color highlight, Color textColor, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
		{
			var orgColor = GUI.color;
			var textSize = Text.Font;
			Text.Font = fontSize;
			GUI.color = highlight;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = textColor;

			var anchorTmp = Text.Anchor;
			Text.Anchor = anchor;
			Widgets.Label(rect, label);
			Text.Font = textSize;
			Text.Anchor = anchorTmp;
			GUI.color = orgColor;
		}

		public static bool ClickableLabel(Rect rect, string label, Color mouseOver, Color textColor, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
		{
			var color = GUI.color;
			var textSize = Text.Font;
			Text.Font = fontSize;
			var textAnchor = Text.Anchor;
			if (Mouse.IsOver(rect))
			{
				GUI.color = mouseOver;
			}
			else
			{
				GUI.color = textColor;
			}
			Widgets.Label(rect, label);

			GUI.color = color;
			Text.Font = textSize;
			Text.Anchor = textAnchor;

			return Widgets.ButtonInvisible(rect);
		}

		public static void DoTooltipRegion(Rect rect, string tooltip, bool slider = false)
		{
			if (slider)
			{
				rect.y -= Mathf.Round(rect.height / 2f) + 3;
			}
			TooltipHandler.TipRegion(rect, tooltip);
		}

		public static void DrawLineHorizontalGrey(float x, float y, float length)
		{
			GUI.DrawTexture(new Rect(x, y, length, 1f), BaseContent.GreyTex);
		}

		//public static void DrawLineTo(Vector3 origin, float angle, float distance)
		//{
		//	Quaternion quat = Quaternion.LookRotation(Vector3.Ang);
		//	Vector3 s = new Vector3(0.2f, 1f, z);
		//	Matrix4x4 matrix = default(Matrix4x4);
		//	matrix.SetTRS(pos, q, s);
		//	Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
		//}

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
	}
}
