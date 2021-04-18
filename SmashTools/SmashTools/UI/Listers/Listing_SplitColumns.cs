using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace SmashTools
{
    public class Listing_SplitColumns : Listing
    {
        protected const float DefSelectionLineHeight = 21f;
        protected const float ColumnSplitWidth = 0.05f;
        protected readonly GameFont font;
        protected int columns = 2;
        protected int curColumn;
        public bool shiftRectScrollbar;

        public Listing_SplitColumns(GameFont font)
		{
			this.font = font;
		}

		public Listing_SplitColumns()
		{
			font = GameFont.Tiny;
		}

        protected int CurrentColumn => curColumn % columns;

		public override void Begin(Rect rect)
		{
            if (shiftRectScrollbar)
            {
                rect.width -= 10;
            }
			base.Begin(rect);
			Text.Font = font;
            curColumn = 0;
		}

        public void Begin(Rect rect, int columns)
        {
            Begin(rect);
            this.columns = columns;
        }

        public void BeginScrollView(Rect rect, ref Vector2 scrollPosition, ref Rect viewRect, int columns)
        {
	        Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
	        rect.height = 100000f;
	        Begin(rect, columns);
        }

		public override void End()
		{
			base.End();
		}

        public void EndScrollView(ref Rect viewRect)
        {
	        viewRect = new Rect(0f, 0f, listingRect.width, curY);
	        Widgets.EndScrollView();
	        End();
        }

        public virtual void NextRow(float gapHeight = 16)
        {
            if (curColumn > 0)
            {
                curColumn = 0;
                curX = 0;
                curY += gapHeight;
            }
        }

        public Rect GetSplitRect(float height)
        {
	        NewColumnIfNeeded(height);
	        Rect result = new Rect(curX + 2, curY, ColumnWidth / (columns + (columns * columns * ColumnSplitWidth)) - 4, height);
	        return result;
        }

        public virtual void Shift(float gapHeight = 34)
        {
            if (curColumn > 0)
            {
                if (CurrentColumn == 0)
                {
                    curX = 0;
                    curY += gapHeight;
                }
                else
                {
                    curX = ColumnWidth / (columns - (columns * ColumnSplitWidth)) * CurrentColumn;
                }

            }
            curColumn++;
        }

        public void Header(string header, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            Color highlight = ListingExtension.BannerColor;
            var textSize = Text.Font;
            Text.Font = fontSize;

            Rect rect = GetRect(Text.CalcHeight(header, ColumnWidth));
            GUI.color = highlight;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = ListingExtension.TextColor;
            
            var anchorTmp = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(rect, header);
            Text.Font = textSize;   
            Text.Anchor = anchorTmp;

            Gap(16);
        }

        public void CheckboxLabeled(string label, ref bool checkState, string tooltip, string disabledTooltip, bool locked)
		{
            Shift();
			Rect rect = GetSplitRect(Text.LineHeight);
            rect.y -= rect.height / 2;
            bool disabled = !disabledTooltip.NullOrEmpty();
            if (disabled)
            {
                UIElements.DoTooltipRegion(rect, disabledTooltip);
            }
			else if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
                UIElements.DoTooltipRegion(rect, tooltip);
			}
            if (locked)
            {
                checkState = false;
            }
			UIElements.CheckboxLabeled(rect, label, ref checkState, disabled, null, null, false);
		}

		public void IntegerBox(string label, ref int value, string tooltip, string disabledTooltip, int min = int.MinValue, int max = int.MaxValue)
        {
            Shift();
            Rect rect = GetSplitRect(Text.LineHeight);
            float centerY = rect.y - rect.height / 2;
            float length = rect.width * 0.45f;
            Rect rectLeft = new Rect(rect.x, centerY, length, rect.height);
            Rect rectRight = new Rect(rect.x + (rect.width - length), centerY, length, rect.height);

            Color color = GUI.color;
            if (!disabledTooltip.NullOrEmpty())
            {
                GUI.color = UIElements.InactiveColor;
                GUI.enabled = false;
                UIElements.DoTooltipRegion(rect, disabledTooltip);
            }
            else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip);
			}
            Widgets.Label(rectLeft, label);

            var align = Text.CurTextFieldStyle.alignment;
            Text.CurTextFieldStyle.alignment = TextAnchor.MiddleRight;
            string buffer = value.ToString();
            Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

            Text.CurTextFieldStyle.alignment = align;
            GUI.color = color;
            GUI.enabled = true;
        }

        public void FloatBox(string label, ref float value, string tooltip, string disabledTooltip, float min = int.MinValue, float max = int.MaxValue)
        {
            Shift();
            Rect rect = GetSplitRect(Text.LineHeight);
            float centerY = rect.y - rect.height / 2;
            float length = rect.width * 0.45f;
            Rect rectLeft = new Rect(rect.x, centerY, length, rect.height);
            Rect rectRight = new Rect(rect.x + (rect.width - length), centerY, length, rect.height);

            Color color = GUI.color;
            if (!disabledTooltip.NullOrEmpty())
            {
                GUI.color = UIElements.InactiveColor;
                GUI.enabled = false;
                UIElements.DoTooltipRegion(rect, disabledTooltip);
            }
            else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip);
			}
            Widgets.Label(rectLeft, label);

            var align = Text.CurTextFieldStyle.alignment;
            Text.CurTextFieldStyle.alignment = TextAnchor.MiddleRight;
            string buffer = value.ToString();

            Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

            Text.CurTextFieldStyle.alignment = align;
            GUI.color = color;
            GUI.enabled = true;
        }

        public void SliderPercentLabeled(string label, ref float value, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
            float endValue = -1f, string endValueDisplay = "", bool translate = false)
        {
            Shift();
            Rect rect = GetSplitRect(24f);
            string format = $"{Math.Round(value * 100, decimalPlaces)}" + endSymbol;
            if (!endValueDisplay.NullOrEmpty() && endValue > 0)
            {
                if (value >= endValue)
                {
                    format = endValueDisplay;
                    if (translate)
                    {
                        format = format.Translate();
                    }
                }
            }
            Color color = GUI.color;
            if (!disabledTooltip.NullOrEmpty())
            {
                GUI.color = UIElements.InactiveColor;
                GUI.enabled = false;
                UIElements.DoTooltipRegion(rect, disabledTooltip);
            }
			else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip, true);
			}
            value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            float value2 = value;
            if (endValue > 0 && value2 >= max)
            {
                value2 = endValue;
            }
            GUI.enabled = true;
            GUI.color = color;
        }

        public void SliderLabeled(string label, ref float value, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
            float endValue = -1f, float increment = 0, string endValueDisplay = "", bool translate = false)
        {
            Shift();
            Rect rect = GetSplitRect(24f);
            string format = $"{Math.Round(value, decimalPlaces)}" + endSymbol;
            if (!endValueDisplay.NullOrEmpty())
            {
                if (value >= max)
                {
                    format = endValueDisplay;
                    if (translate)
                    {
                        format = format.Translate();
                    }
                }
            }
            Color color = GUI.color;
            if (!disabledTooltip.NullOrEmpty())
            {
                GUI.color = UIElements.InactiveColor;
                GUI.enabled = false;
                UIElements.DoTooltipRegion(rect, disabledTooltip);
            }
			else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip, true);
			}
            value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            float value2 = value;
            if (increment > 0)
            {
                value = value.RoundTo(increment);
                value2 = value2.RoundTo(increment);
            }
            if (endValue > 0 && value2 >= max)
            {
                value2 = endValue;
            }
            GUI.color = color;
            GUI.enabled = true;
        }

        public void SliderLabeled(string label, ref int value, string tooltip, string disabledTooltip, string endSymbol, int min, int max, 
            int endValue = -1, string maxValueDisplay = "", string minValueDisplay = "", bool translate = false)
        {
            Shift();
            Rect rect = GetSplitRect(24f);
            string format = string.Format("{0}" + endSymbol, value);
            if (!maxValueDisplay.NullOrEmpty())
            {
                if (value == max)
                {
                    format = maxValueDisplay;
                    if (translate)
                    {
                        format = format.Translate();
                    }
                }
            }
            if (!minValueDisplay.NullOrEmpty())
            {
                if (value == min)
                {
                    format = minValueDisplay;
                    if (translate)
                    {
                        format = format.Translate();
                    }
                }
            }
            Color color = GUI.color;
            if (!disabledTooltip.NullOrEmpty())
            {
                GUI.color = UIElements.InactiveColor;
                GUI.enabled = false;
                UIElements.DoTooltipRegion(rect, disabledTooltip);
            }
            else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip, true);
			}
            value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            int value2 = value;
            if (value2 >= max && endValue > 0)
            {
                value2 = endValue;
            }
            GUI.color = color;
            GUI.enabled = true;
        }

        public void EnumSliderLabeled(string label, ref int value, string tooltip, string disabledTooltip, Type enumType, bool translate = false)
        {
            Shift();
            try
            {
                int[] enumValues = Enum.GetValues(enumType).Cast<int>().ToArray();
                string[] enumNames = Enum.GetNames(enumType);
                int min = enumValues[0];
                int max = enumValues.Last();
                Rect rect = GetSplitRect(24f);
                string format = Enum.GetName(enumType, value);
                if (translate)
                {
                    format = format.Translate();
                }
                Color color = GUI.color;
                if (!disabledTooltip.NullOrEmpty())
                {
                    GUI.color = UIElements.InactiveColor;
                    GUI.enabled = false;
                    UIElements.DoTooltipRegion(rect, disabledTooltip);
                }
                else if (!tooltip.NullOrEmpty())
			    {
				    UIElements.DoTooltipRegion(rect, tooltip, true);
			    }
                value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
                GUI.color = color;
                GUI.enabled = true;
            }
            catch(Exception ex)
            {
                Log.Error($"Unable to convert to {enumType}. Exception={ex.Message}");
                return;
            }
        }

        public void FillableBarLabeled(float fillPercent, string label, Texture2D fillTex, Texture2D addedFillTex, Texture2D innerTex, Texture2D outlineTex, float? actualValue = null, float addedValue = 0f, float bgFillPercent = 0f, float[] thresholds = null)
        {
	        if (fillPercent < 0f)
	        {
		        fillPercent = 0f;
	        }
	        if (fillPercent > 1f)
	        {
		        fillPercent = 1f;
	        }
            Shift();
            if (fillTex != null && addedFillTex != null)
            {
                var anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect rect = GetSplitRect(24);
                UIElements.FillableBarHollowed(rect, fillPercent, bgFillPercent, fillTex, addedFillTex, innerTex, outlineTex);
            
                Rect rectLabel = rect;
                rectLabel.x += 5f;

                GUIStyle style = new GUIStyle(Text.CurFontStyle);

                UIElements.LabelOutlineStyled(rectLabel, label, style, Color.black);
                if (thresholds != null)
                {
                    foreach (float threshold in thresholds)
                    {
                        UIElements.DrawBarThreshold(rect, threshold, fillPercent);
                    }
                }

                if(actualValue != null)
                {
                    Rect valueRect = rect;
                    valueRect.width /= 2;
                    valueRect.x = rectLabel.x + rectLabel.width / 2 - 6f;
                    style.alignment = TextAnchor.MiddleRight;

                    string value = string.Format("{1} {0}", actualValue.ToString(), addedValue != 0 ? "(" + (addedValue > 0 ? "+" : "") +  addedValue.ToString() + ")" : "");
                    UIElements.LabelOutlineStyled(valueRect, value, style, Color.black);
                }
                Text.Anchor = anchor;
            }
        }

        public Rect Label(string label, float maxHeight = -1f, string tooltip = null)
        {
            float num = Text.CalcHeight(label, ColumnWidth);
            Shift(num);
	        if (maxHeight >= 0f && num > maxHeight)
	        {
		        num = maxHeight;
	        }
	        Rect rect = GetSplitRect(num);
	        Widgets.Label(rect, label);
	        if (tooltip != null)
	        {
		        TooltipHandler.TipRegion(rect, tooltip);
	        }
	        return rect;
        }
    }
}
