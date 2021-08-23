using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	public class Dialog_DebugCheckboxLister : Dialog_OptionLister
	{
		public List<DebugCheckboxLister> listOptions = new List<DebugCheckboxLister>();

		public Dialog_DebugCheckboxLister(List<DebugCheckboxLister> listOptions) : base()
		{
			forcePause = true;
			this.listOptions = new List<DebugCheckboxLister>(listOptions);
		}

		protected override void DoListingItems()
		{
			foreach (DebugCheckboxLister listCheckbox in listOptions)
			{
				DebugCheckbox(listCheckbox);
			}
		}

		protected void DebugCheckbox(DebugCheckboxLister listCheckbox)
		{
			if (!FilterAllows(listCheckbox.label))
			{
				GUI.color = new Color(1f, 1f, 1f, 0.3f);
			}
			bool beforeValue = listCheckbox.checkOn();
			bool editValue = beforeValue;
			listing.CheckboxLabeled(listCheckbox.label, ref editValue);
			if (beforeValue != editValue)
			{
				beforeValue = editValue;
				listCheckbox.checkAction?.Invoke();
			}
			GUI.color = Color.white;
		}

		public struct DebugCheckboxLister
		{
			public string label;
			public Func<bool> checkOn;
			public Action checkAction;

			public DebugCheckboxLister(string label, Func<bool> checkOn, Action checkAction = null)
			{
				this.label = label;
				this.checkOn = checkOn;
				this.checkAction = checkAction;
			}
		}
	}
}
