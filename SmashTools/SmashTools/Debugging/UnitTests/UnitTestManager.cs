using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools.Debugging
{
	public static class UnitTestManager
	{
		private static List<UnitTest> unitTests = new List<UnitTest>();

		private static void RunAll()
		{
			Parallel.ForEach(GenTypes.AllTypes, type =>
			{
				if (type.IsSubclassOf(typeof(UnitTest)) && !type.IsAbstract)
				{
					lock (unitTests)
					{
						unitTests.Add((UnitTest)Activator.CreateInstance(type));
					}
				}
			});

			ExecuteTests();
		}

		public static void ExecuteTests()
		{
			Log.Message($"---------- Unit Tests ----------");
			List<string> results = new List<string>();
			foreach (UnitTest unitTest in unitTests)
			{
				try
				{
					bool success = true;
					results.Clear();
					foreach (Func<UTResult> test in unitTest.Execute())
					{
						string output;
						try
						{
							UTResult result = test();
							output = result.Passed ? $"    {result.Name} <success>Passed</success>" : $"    {result.Name} <error>Failed</error>";
							// Dump all results for this unit test if any sub-test fails
							success &= result.Passed; 
						}
						catch (Exception ex)
						{
							output = $"<error>Exception thrown!</error>\n{ex}";
						}
						results.Add(output);
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
		}

		private static void DumpResults(List<string> results)
		{
			foreach (string result in results)
			{
				SmashLog.Message($"    {result}");
			}
		}

		[StartupAction(Category = "UnitTesting", Name = "Run All", GameState = GameState.OnStartup)]
		private static void RunAllTests()
		{
			RunAll();
		}
	}
}
