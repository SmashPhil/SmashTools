using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Profile;
using static SmashTools.Debug;

namespace SmashTools.Debugging
{
	public static class UnitTestManager
	{
		private static bool runningUnitTests;
		private static List<UnitTest> unitTests = new List<UnitTest>();
		private static List<string> results = new List<string>();

		public static event Action<bool> onUnitTestStateChange;
		
		static UnitTestManager()
		{
			ConcurrentBag<UnitTest> tests = new ConcurrentBag<UnitTest>();
			Parallel.ForEach(GenTypes.AllTypes, type =>
			{
				if (type.IsSubclassOf(typeof(UnitTest)) && !type.IsAbstract)
				{
					tests.Add((UnitTest)Activator.CreateInstance(type));
				}
			});
			unitTests.AddRange(tests);
		}

		public static UnitTest.TestType TestType { get; private set; }

		public static bool RunningUnitTests
		{
			get
			{
				return runningUnitTests;
			}
			set
			{
				if (runningUnitTests == value) return;

				runningUnitTests = value;
				onUnitTestStateChange?.Invoke(runningUnitTests);
			}
		}

#if DEBUG
		[StartupAction(Category = "UnitTesting", Name = "Run All", GameState = GameState.OnStartup)]
		public static void RunAll()
		{
			TestType = UnitTest.TestType.MainMenu | UnitTest.TestType.GameLoaded;
			StartUnitTests();
		}

		[StartupAction(Category = "UnitTesting", Name = "Run MainMenu Tests", GameState = GameState.OnStartup)]
		public static void RunMainMenuTests()
		{
			TestType = UnitTest.TestType.MainMenu;
			StartUnitTests();
		}

		[StartupAction(Category = "UnitTesting", Name = "Run Game Tests", GameState = GameState.OnStartup)]
		public static void RunGameTests()
		{
			TestType = UnitTest.TestType.GameLoaded;
			StartUnitTests();
		}

		private static void StartUnitTests()
		{
			if (ModsConfig.IsActive("UnlimitedHugs.HugsLib"))
			{
				Type type = AccessTools.TypeByName("HugsLib.Quickstart.QuickstartController");
				Assert(type != null, "Can't find QuickstartController type");
				MethodInfo abortMethod = AccessTools.Method(type, "StatusBoxAbortRequestedHandler");
				Assert(abortMethod != null, "Can't find abort method.");
				abortMethod.Invoke(null, new object[] { false });
			}
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				CoroutineManager.QueueInvoke(UnitTestRoutine);
			});
		}
#endif

		private static IEnumerator UnitTestRoutine()
		{
			if (TestType == UnitTest.TestType.None) yield break;

			RunningUnitTests = true;

			results.Clear();
			results.Add($"---------- Unit Tests ----------");
			if (TestType.HasFlag(UnitTest.TestType.MainMenu))
			{
				if (Current.ProgramState != ProgramState.Entry)
				{
					GenScene.GoToMainMenu();
				}
				while (Current.ProgramState != ProgramState.Entry || LongEventHandler.AnyEventNowOrWaiting)
				{
					if (!RunningUnitTests) goto EndTest;
					yield return new WaitForSecondsRealtime(1);
				}
				bool result = ExecuteTests(UnitTest.TestType.MainMenu, results);
				if (!result) goto EndTest;
			}
			
			if (TestType.HasFlag(UnitTest.TestType.GameLoaded))
			{
				LongEventHandler.QueueLongEvent(delegate ()
				{
					Root_Play.SetupForQuickTestPlay();
					PageUtility.InitGameStart();
				}, "GeneratingMap", true, delegate (Exception ex)
				{
					GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap(ex);
					RunningUnitTests = false;
				}, true, null);

				while (Current.ProgramState != ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting)
				{
					if (!RunningUnitTests) goto EndTest;
					yield return new WaitForSecondsRealtime(1);
				}
				ExecuteTests(UnitTest.TestType.GameLoaded, results);
			}

			EndTest:;
			results.Add($"-------- End Unit Tests --------");
			DumpResults(results);

			RunningUnitTests = false;
			TestType = UnitTest.TestType.None;
		}

		/// <returns>Should continue running Unit Tests</returns>
		private static bool ExecuteTests(UnitTest.TestType type, List<string> results)
		{
			bool success = true;
			List<string> subResults = new List<string>();
			foreach (UnitTest unitTest in unitTests)
			{
				if (unitTest.ExecuteOn != type) continue;

				try
				{
					foreach (Func<UTResult> test in unitTest.Execute())
					{
						string output;
						try
						{
							UTResult result = test();
							foreach ((string name, bool passed) in result.Results)
							{
								output = passed ? $"    {name} <success>Passed</success>" : $"    {name} <error>Failed</error>";
								// Dump all results for this unit test if any sub-test fails
								success &= passed;
								subResults.Add(output);
							}
						}
						catch (Exception ex)
						{
							output = $"<error>Exception thrown!</error>\n{ex}";
							success = false;
							subResults.Add(output);
						}
					}
					if (success)
					{
						results.Add($"[{unitTest.Name}] <success>{subResults.Count} Succeeded</success>");
					}
					else
					{
						results.Add($"<error>{unitTest.Name} Failed</error>");
						results.AddRange(subResults);
					}
				}
				catch (Exception ex)
				{
					SmashLog.Message($"<error>[{unitTest.Name} Exception thrown!]</error>\n{ex}");
					continue;
				}
				subResults.Clear();
			}
			return success;
		}

		private static void DumpResults(List<string> results)
		{
			foreach (string result in results)
			{
				SmashLog.Message(result);
			}
		}
	}
}
