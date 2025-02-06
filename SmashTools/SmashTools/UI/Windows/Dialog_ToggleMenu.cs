using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_ToggleMenu : Window
	{
		protected readonly Listing_Standard lister;

		protected string label;
		protected List<Toggle> toggles;
		protected Action postClose;

		public Dialog_ToggleMenu(string label, List<Toggle> toggles, Action postClose = null)
		{
			this.label = label;
			this.toggles = toggles.OrderBy(rb => rb.Category).ToList();
			this.postClose = postClose;

			doCloseX = true;
			onlyOneOfTypeAllowed = true;
			absorbInputAroundWindow = true;
			lister = new Listing_Standard(GameFont.Small)
			{
				ColumnWidth = 300
			};
		}

		public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

		public override void PostClose()
		{
			postClose?.Invoke();
		}

		protected virtual void DrawToggle()
		{
			string category = string.Empty;
			for (int i = 0; i < toggles.Count; i++)
			{
				Toggle toggle = toggles[i];
				if (toggle.Category != category)
				{
					category = toggle.Category;
					lister.Header(category, ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
				}
				bool checkOn = toggle.Active;
				using (new TextBlock(Color.white))
				{
					if (toggle.Disabled)
					{
						GUI.enabled = false;
						GUI.color = UIElements.InactiveColor;
					}
					lister.CheckboxLabeled(toggle.DisplayName, ref checkOn);
				}
				GUI.enabled = true;
				if (!toggle.Disabled && toggle.Active != checkOn)
				{
					toggle.OnToggle(checkOn);
				}
				toggle.Active = checkOn;
			}
		}

		public override void DoWindowContents(Rect rect)
		{
			using (new TextBlock(GameFont.Medium, TextAnchor.UpperCenter))
			{
				Widgets.Label(rect, label);
				rect.y += 30;
			}

			lister.Begin(rect);
			{
				DrawToggle();
			}
			lister.End();
		}
	}
}
