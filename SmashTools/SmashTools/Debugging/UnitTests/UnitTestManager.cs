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
	internal static class UnitTestManager
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

		private static void ExecuteTests()
		{
			Log.Message($"---------- Unit Tests ----------");
			foreach (UnitTest unitTest in unitTests)
			{
				SmashLog.Message($"Running {unitTest.Name}");
				foreach (UTResult result in unitTest.Execute())
				{
					string output;
					try
					{
						output = result.Passed ? "<success>Success</success>" : "<error>Failed</error>";
					}
					catch (Exception ex)
					{
						output = $"<error>{ex.GetType().Name}</error>\n{ex}";
					}
					SmashLog.Message($"    {result.Name}{output}");
				}
			}
			Log.Message($"-------- End Unit Tests --------");
		}

		[StartupAction(Category = "UnitTesting", Name = "Run All", GameState = GameState.OnStartup)]
		private static void RunAllTests()
		{
			RunAll();
		}
	}
}
