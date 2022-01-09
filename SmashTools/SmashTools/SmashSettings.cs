using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public class SmashSettings : IExposable
	{
		public Dictionary<string, bool> unitTests = new Dictionary<string, bool>();

		public static string FullPath => Path.Combine(GenFilePaths.ConfigFolderPath, "SmashTools.xml");

		public void EnableUnitTest(string fullName, bool checkOn)
		{
			if (!unitTests.NullOrEmpty())
			{
				if (!unitTests.ContainsKey(fullName))
				{
					unitTests.Add(fullName, false);
				}
				unitTests = unitTests.ToDictionary(p => p.Key, p => false);
				unitTests[fullName] = checkOn;
			}
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref unitTests, "unitTests", LookMode.Value, LookMode.Value);
		}
	}

	public class SmashMod : Mod
	{
		public static SmashSettings settings;
		public static SmashMod mod;

		public SmashMod(ModContentPack modContentPack) : base(modContentPack)
		{
			mod = this;
			settings = new SmashSettings();
		}

		public static void Serialize()
		{
			Scribe.saver.InitSaving(SmashSettings.FullPath, "SmashSettings");
			try
			{
				Scribe_Deep.Look(ref settings, "SmashSettings");
			}
			finally
			{
				Scribe.saver.FinalizeSaving();
			}
		}

		public static void LoadFromSettings()
		{
			if (File.Exists(SmashSettings.FullPath))
			{
				Scribe.loader.InitLoading(SmashSettings.FullPath);
				try
				{
					Scribe_Deep.Look(ref settings, "SmashSettings");
				}
				finally
				{
					Scribe.loader.FinalizeLoading();
				}
			}
		}
	}
}
