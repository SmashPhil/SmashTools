﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class UnitTesting
	{
		internal static readonly Dictionary<GameState, List<Action>> postLoadActions = new Dictionary<GameState, List<Action>>();
		internal static readonly Dictionary<string, UnitTestAction> unitTests = new Dictionary<string, UnitTestAction>();
		internal static readonly List<Toggle> unitTestRadioButtons = new List<Toggle>();

		internal static bool NoUnitTest { get; private set; }
		internal static bool Enabled { get; private set; }

		static UnitTesting()
		{
			Enabled = false;
			try
			{
				InitializeUnitTesting();
			}
			catch (Exception ex1)
			{
				Log.Warning($"UnitTest startup threw exception. Clearing config for unit testing and trying again. Exception={ex1.Message}");
				Utilities.DeleteSettings();
				try
				{
					InitializeUnitTesting();
				}
				catch (Exception ex2)
				{
					SmashLog.ErrorLabel("[SmashLog]", $"UnitTest startup was unable to initialize. Disabling unit tests. Exception={ex2.Message}");
					return;
				}
			}
			Enabled = true;
			PostLoadSetup();
		}

		public static void OpenMenu()
		{
			Find.WindowStack.Add(new Dialog_RadioButtonMenu("Unit Testing", unitTestRadioButtons, postClose: SmashMod.Serialize));
		}

		private static void InitializeUnitTesting()
		{
			SmashMod.LoadFromSettings();
			postLoadActions.Clear();
			foreach (GameState enumValue in Enum.GetValues(typeof(GameState)))
			{
				postLoadActions.Add(enumValue, new List<Action>());
			}
			unitTests.Clear();
			unitTestRadioButtons.Clear();
			NoUnitTest = !SmashSettings.unitTests.Any(kvp => kvp.Value);
			unitTestRadioButtons.Add(new Toggle("NoUnitTest", "None", string.Empty, () => NoUnitTest, (value) => NoUnitTest = value));
			List<MethodInfo> methods = new List<MethodInfo>();
			foreach (Type type in GenTypes.AllTypes)
			{
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => !m.GetParameters().Any()))
				{
					if (method.GetCustomAttribute<UnitTestAttribute>() is UnitTestAttribute unitTestAttr)
					{
						string name = unitTestAttr.Name;
						if (string.IsNullOrEmpty(name))
						{
							name = method.Name;
						}
						string category = unitTestAttr.Category;
						if (string.IsNullOrEmpty(category))
						{
							category = "General";
						}

						string unitTestFullName = $"{category}.{name}";
						SmashSettings.unitTests.TryAdd(unitTestFullName, false);
						UnitTestAction unitTest = new UnitTestAction()
						{
							FullName = unitTestFullName,
							DisplayName = name,
							Category = category,
							GameState = unitTestAttr.GameState,
							Action = () => method.Invoke(null, new object[] { })
						};
						unitTests.Add(unitTestFullName, unitTest);
						unitTestRadioButtons.Add(new Toggle(unitTest.FullName, unitTest.DisplayName, unitTest.Category,
							() => SmashSettings.unitTests.TryGetValue(unitTest.FullName, false),
							(value) => SmashSettings.unitTests[unitTest.FullName] = value));
					}
				}
			}
		}

		public static void DrawDebugWindowButton(WidgetRow ___widgetRow)
		{
			if (___widgetRow.ButtonIcon(TexButton.OpenDebugActionsMenu, "Open Unit Testing menu.\n\n This lets you initiate certain static methods on startup for quick testing."))
			{
				OpenMenu();
			}
		}

		private static void PostLoadSetup()
		{
			foreach (var unitTestSaveData in SmashSettings.unitTests)
			{
				if (unitTestSaveData.Value && unitTests.TryGetValue(unitTestSaveData.Key, out UnitTestAction unitTest))
				{
					postLoadActions[unitTest.GameState].Add(unitTest.Action);
				}
			}
		}

		public static void ExecutePostLoadTesting()
		{
			ExecuteTesting(GameState.LoadedSave);
			ExecuteTesting(GameState.Playing);
		}

		public static void ExecuteNewGameTesting()
		{
			ExecuteTesting(GameState.NewGame);
			ExecuteTesting(GameState.Playing);
		}

		public static void ExecuteOnStartupTesting()
		{
			ExecuteTesting(GameState.OnStartup);
		}

		private static void ExecuteTesting(GameState gameState)
		{
			if (Enabled)
			{
				foreach (Action action in postLoadActions[gameState])
				{
					action.Invoke();
				}
			}
		}

		internal struct UnitTestAction
		{
			public string FullName { get; set; }
			public string DisplayName { get; set; }
			public string Category { get; set; }
			public Action Action { get; set; }
			public GameState GameState { get; set; }
		}
	}
}
