using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_RadioButtonMenu : Dialog_ToggleMenu
	{
		public Dialog_RadioButtonMenu(string label, List<Toggle> toggles, Action postClose = null) : base(label, toggles, postClose)
		{
		}

		protected override void DrawToggle()
		{
			string category = string.Empty;
			(int activeIndex, bool toggled) state = (-1, false);
			for (int i = 0; i < toggles.Count; i++)
			{
				Toggle radioButton = toggles[i];
				if (radioButton.Category != category)
				{
					category = radioButton.Category;
					lister.Header(category, ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
				}
				if (radioButton.Disabled)
				{
					GUIState.Disable();
				}
				bool value = lister.RadioButton(radioButton.DisplayName, radioButton.Active);
				GUIState.Enable();
				if (!radioButton.Disabled && value)
				{
					state.activeIndex = i;
					state.toggled = true;
				}
				else if (radioButton.Active && state.activeIndex < 0)
				{
					state.activeIndex = i;
				}
				radioButton.Active = false;
			}
			if (!toggles.OutOfBounds(state.activeIndex))
			{
				toggles[state.activeIndex].Active = true;
				if (state.toggled)
				{
					toggles[state.activeIndex].OnToggle(true);
				}
			}
		}
	}
}
