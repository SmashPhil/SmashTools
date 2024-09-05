using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class StartupTest
	{
		internal static Dictionary<GameState, List<Action>> postLoadActions = new Dictionary<GameState, List<Action>>();
		internal static Dictionary<string, UnitTestAction> unitTests = new Dictionary<string, UnitTestAction>();
		internal static List<Toggle> unitTestRadioButtons = new List<Toggle>();

		internal static bool NoUnitTest { get; private set; }

		internal static bool Enabled { get; private set; }

		static StartupTest()
		{
#if DEBUG
			Enabled = false;
			try
			{
				InitializeUnitTesting();
			}
			catch (Exception ex1)
			{
				Log.Warning($"UnitTest startup threw exception. Clearing config for startup actions and trying again. Exception={ex1.Message}");
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
#endif
		}

		[Conditional("DEBUG")]
		public static void OpenMenu()
		{
			Find.WindowStack.Add(new Dialog_RadioButtonMenu("Startup Actions", unitTestRadioButtons, postClose: SmashMod.Serialize));
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
			unitTestRadioButtons.Add(new Toggle("NoUnitTest", "None", string.Empty, () => NoUnitTest || SmashSettings.startupAction.NullOrEmpty(), delegate (bool value)
			{
				NoUnitTest = value;
				if (NoUnitTest)
				{
					SmashSettings.startupAction = string.Empty;
				}
			}));
			NoUnitTest = true;
			List<MethodInfo> methods = new List<MethodInfo>();
			foreach (Type type in GenTypes.AllTypes)
			{
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => !m.GetParameters().Any()))
				{
					if (method.GetCustomAttribute<StartupActionAttribute>() is StartupActionAttribute unitTestAttr)
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

						string unitTestFullName = $"{category}.{name}".Replace(" ", "");
						UnitTestAction unitTest = new UnitTestAction()
						{
							FullName = unitTestFullName,
							DisplayName = name,
							Category = category,
							GameState = unitTestAttr.GameState,
							Action = () => method.Invoke(null, new object[] { })
						};

						if (unitTestFullName == SmashSettings.startupAction)
						{
							NoUnitTest = false;
						}

						unitTests.Add(unitTest.FullName, unitTest);
						unitTestRadioButtons.Add(new Toggle(unitTest.FullName, unitTest.DisplayName, unitTest.Category,
							stateGetter: delegate ()
							{
								return SmashSettings.startupAction == unitTest.FullName;
							},
							stateSetter: delegate (bool value)
							{
								if (value)
								{
									SmashSettings.startupAction = unitTest.FullName;
								}
							}));
					}
				}
			}
			unitTestRadioButtons = unitTestRadioButtons.OrderBy(toggle => toggle.DisplayName).ToList();
		}

		private static void PostLoadSetup()
		{
			if (!SmashSettings.startupAction.NullOrEmpty() && unitTests.TryGetValue(SmashSettings.startupAction, out UnitTestAction unitTest))
			{
				postLoadActions[unitTest.GameState].Add(unitTest.Action);
			}
		}

		internal static void ExecutePostLoadTesting()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				ExecuteTesting(GameState.LoadedSave);
				ExecuteTesting(GameState.Playing);
			});
		}

		internal static void ExecuteNewGameTesting()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				ExecuteTesting(GameState.NewGame);
				ExecuteTesting(GameState.Playing);
			});
		}

		internal static void ExecuteOnStartupTesting()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				ExecuteTesting(GameState.OnStartup);
			});
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
