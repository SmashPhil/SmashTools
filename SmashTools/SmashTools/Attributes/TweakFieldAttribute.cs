using System;
using System.Collections.Generic;
using System.Linq;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false)]
	public class TweakFieldAttribute : Attribute
	{
		public TweakFieldAttribute()
		{
		}

		public string Category { get; set; }

		public UISettingsType SettingsType { get; set; } = UISettingsType.None;
	}
}
