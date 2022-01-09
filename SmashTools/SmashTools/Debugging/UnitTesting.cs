using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace SmashTools.Debugging
{
	[StaticConstructorOnStartup]
	public static class UnitTesting
	{
		internal static readonly Dictionary<GameState, List<Action>> postLoadActions = new Dictionary<GameState, List<Action>>();
		internal static readonly Dictionary<string, UnitTestAction> unitTests = new Dictionary<string, UnitTestAction>();
		internal static readonly Dictionary<string, List<string>> unitTestCategories = new Dictionary<string, List<string>>();

		private static bool Enabled { get; set; }

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

		private static void InitializeUnitTesting()
		{
			SmashMod.LoadFromSettings();
			postLoadActions.Clear();
			foreach (GameState enumValue in Enum.GetValues(typeof(GameState)))
			{
				postLoadActions.Add(enumValue, new List<Action>());
			}
			unitTests.Clear();
			List<MethodInfo> methods = new List<MethodInfo>();
			foreach (Type type in GenTypes.AllTypes)
			{
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => !m.GetParameters().Any()))
				{
					if (method.GetCustomAttribute<UnitTestAttribute>() is UnitTestAttribute unitTest)
					{
						string name = unitTest.Name;
						if (string.IsNullOrEmpty(name))
						{
							name = method.Name;
						}
						string category = unitTest.Category;
						if (string.IsNullOrEmpty(category))
						{
							category = "General";
						}

						string unitTestFullName = $"{unitTest.Category}.{name}";
						unitTestCategories.AddOrInsert(category, unitTestFullName);
						SmashMod.settings.unitTests.TryAdd(unitTestFullName, false);
						unitTests.Add(unitTestFullName, new UnitTestAction()
						{
							FullName = unitTestFullName,
							DisplayName = name,
							GameState = unitTest.GameState,
							Action = () => method.Invoke(null, new object[] { })
						});
					}
				}
			}
		}

		public static void DrawDebugWindowButton(WidgetRow ___widgetRow)
		{
			if (___widgetRow.ButtonIcon(TexButton.OpenDebugActionsMenu, "Open Unit Testing menu.\n\n This lets you initiate certain static methods on startup for quick testing."))
			{
				Find.WindowStack.Add(new Dialog_UnitTesting());
			}
		}

		private static void PostLoadSetup()
		{
			foreach (var unitTestSaveData in SmashMod.settings.unitTests)
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
			public string DisplayName { get; set; }

			public string FullName { get; set; }

			public Action Action { get; set; }

			public GameState GameState { get; set; }
		}
	}
}
