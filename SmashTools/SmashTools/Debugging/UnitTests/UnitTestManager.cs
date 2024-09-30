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
			RunningUnitTests = true;
			
			if (Current.ProgramState != ProgramState.Entry)
			{
				GenScene.GoToMainMenu();
			}
			while (Current.ProgramState != ProgramState.Entry || LongEventHandler.AnyEventNowOrWaiting)
			{
				if (!RunningUnitTests) yield break;
				yield return new WaitForSecondsRealtime(1);
			}
			bool result = ExecuteTests(UnitTest.TestType.MainMenu);
			if (!result) yield break;

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
				if (!RunningUnitTests) yield break;
				yield return new WaitForSecondsRealtime(1);
			}
			ExecuteTests(UnitTest.TestType.GameLoaded);

			RunningUnitTests = false;
		}

		/// <returns>Should continue running Unit Tests</returns>
		public static bool ExecuteTests(UnitTest.TestType type)
		{
			bool success = true;
			Log.Message($"---------- Unit Tests ({type}) ----------");
			List<string> results = new List<string>();
			foreach (UnitTest unitTest in unitTests)
			{
				if (unitTest.ExecuteOn != type) continue;

				try
				{
					results.Clear();
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
								results.Add(output);
							}
						}
						catch (Exception ex)
						{
							output = $"<error>Exception thrown!</error>\n{ex}";
							success = false;
							results.Add(output);
						}
					}
					if (success)
					{
						SmashLog.Message($"[{unitTest.Name}] <success>{results.Count} Succeeded</success>");
					}
					else
					{
						SmashLog.Message($"<error>{unitTest.Name} Failed</error>");
						DumpResults(results);
					}
				}
				catch (Exception ex)
				{
					SmashLog.Message($"<error>[{unitTest.Name} Exception thrown!]</error>\n{ex}");
					continue;
				}
			}
			Log.Message($"-------- End Unit Tests --------");

			return success;
		}

		private static void DumpResults(List<string> results)
		{
			foreach (string result in results)
			{
				SmashLog.Message($"    {result}");
			}
		}
	}
}
