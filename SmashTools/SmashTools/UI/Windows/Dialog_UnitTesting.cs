using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools.Debugging;

namespace SmashTools
{
	public class Dialog_UnitTesting : Window
	{
		private readonly Listing_Standard lister;

		public Dialog_UnitTesting()
		{
			doCloseX = true;
			onlyOneOfTypeAllowed = true;
			absorbInputAroundWindow = true;
			lister = new Listing_Standard(GameFont.Small)
			{
				ColumnWidth = 300
			};
		}

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(UI.screenWidth, UI.screenHeight);
			}
		}

		public override void PostClose()
		{
			SmashMod.Serialize();
		}

		public override void DoWindowContents(Rect rect)
		{
			var anchor = Text.Anchor;
			var font = Text.Font;
			Text.Anchor = TextAnchor.UpperCenter;
			Text.Font = GameFont.Medium;
			Widgets.Label(rect, "Unit Testing");
			rect.y += 30;

			Text.Font = font;
			Text.Anchor = anchor;
			lister.Begin(rect);

			foreach (var unitTestByCategory in UnitTesting.unitTestCategories)
			{
				string category = unitTestByCategory.Key;
				List<string> fullNames = unitTestByCategory.Value;
				lister.Header(category, ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
				List<Pair<string, bool>> unitTests = SmashMod.settings.unitTests.Where(u => fullNames.Contains(u.Key)).Select(kvp => new Pair<string, bool>(kvp.Key, kvp.Value)).ToList();
				foreach (var unitTestItem in unitTests)
				{
					string name = UnitTesting.unitTests[unitTestItem.First].DisplayName;
					bool checkOn = unitTestItem.Second;
					bool beforeCheck = checkOn;
					lister.CheckboxLabeled(name, ref checkOn);

					if (checkOn != beforeCheck)
					{
						SmashMod.settings.EnableUnitTest(unitTestItem.First, checkOn);
					}
				}
			}

			lister.End();

			
		}
	}
}
