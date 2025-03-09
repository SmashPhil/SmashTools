using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace SmashTools.Debugging
{
  public static class UnitTestManager
  {
    private static bool runningUnitTests;
    private static Dictionary<UnitTest.TestType, List<UnitTest>> unitTests = [];
    private static List<string> results = [];

    public static event Action<bool> OnUnitTestStateChange;

    static UnitTestManager()
    {
      var tests = new ConcurrentDictionary<UnitTest.TestType, ConcurrentBag<UnitTest>>();
      foreach (UnitTest.TestType testType in Enum.GetValues(typeof(UnitTest.TestType)))
      {
        if (!tests.TryAdd(testType, new ConcurrentBag<UnitTest>()))
        {
          Assert.Fail();
        }
      }
      Parallel.ForEach(GenTypes.AllTypes, type =>
      {
        if (type.IsSubclassOf(typeof(UnitTest)) && !type.IsAbstract)
        {
          UnitTest unitTest = (UnitTest)Activator.CreateInstance(type);
          tests[unitTest.ExecuteOn].Add(unitTest);
        }
      });
      foreach (UnitTest.TestType testType in Enum.GetValues(typeof(UnitTest.TestType)))
      {
        unitTests.Add(testType, new List<UnitTest>(tests[testType].OrderByDescending(test => (int)test.Priority)));
      }
    }

    public static Dictionary<UnitTest.TestType, bool> TestPlan { get; private set; } = [];

    private static UnitTest IsolatedTest { get; set; }

    public static List<UnitTest> AllUnitTests => unitTests.Values.SelectMany(t => t).ToList();

    private static bool StopRequested { get; set; }

    // Supresses startup action unit tests once it has been executed once to avoid infinite test loops when transitioning scenes.
    private static bool UnitTestsExecuted { get; set; }

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
        OnUnitTestStateChange?.Invoke(runningUnitTests);
      }
    }

#if DEBUG
    public static void Run(UnitTest unitTest)
    {
      if (unitTest.ExecuteOn == UnitTest.TestType.Disabled) return;

      IsolatedTest = unitTest;
      ExecuteUnitTests(unitTest.ExecuteOn);
    }

    [StartupAction(Category = "UnitTesting", Name = "Run All", GameState = GameState.OnStartup)]
    private static void RunAll()
    {
      if (UnitTestsExecuted) return;
      ExecuteUnitTests(UnitTest.TestType.MainMenu, UnitTest.TestType.GameLoaded);
    }

    [StartupAction(Category = "UnitTesting", Name = "Run MainMenu Tests", GameState = GameState.OnStartup)]
    private static void RunMainMenuTests()
    {
      if (UnitTestsExecuted) return;
      ExecuteUnitTests(UnitTest.TestType.MainMenu);
    }

    [StartupAction(Category = "UnitTesting", Name = "Run Game Tests", GameState = GameState.OnStartup)]
    private static void RunGameTests()
    {
      if (UnitTestsExecuted) return;
      ExecuteUnitTests(UnitTest.TestType.GameLoaded);
    }

    private static void StartUnitTests()
    {
      if (ModsConfig.IsActive("UnlimitedHugs.HugsLib"))
      {
        Type type = AccessTools.TypeByName("HugsLib.Quickstart.QuickstartController");
        Assert.IsNotNull(type);
        MethodInfo abortMethod = AccessTools.Method(type, "StatusBoxAbortRequestedHandler");
        Assert.IsNotNull(abortMethod);
        abortMethod.Invoke(null, [false]);
      }

      LongEventHandler.ExecuteWhenFinished(delegate ()
      {
        CoroutineManager.QueueInvoke(UnitTestRoutine);
      });
    }

    /// <remarks>Not including any test types will default to running all available tests.</remarks>
    public static void ExecuteUnitTests(params UnitTest.TestType[] testTypes)
    {
      if (testTypes.NullOrEmpty())
        testTypes = [UnitTest.TestType.MainMenu, UnitTest.TestType.GameLoaded];

      UnitTestsExecuted = true;
      EnableForTests(testTypes);
      StartUnitTests();
    }
#endif

    private static void EnableForTests(params UnitTest.TestType[] types)
    {
      TestPlan.Clear();
      foreach (UnitTest.TestType testType in Enum.GetValues(typeof(UnitTest.TestType)))
      {
        TestPlan[testType] = types.Contains(testType);
      }
    }

    private static IEnumerator UnitTestRoutine()
    {
      Assert.IsTrue(!TestPlan.NullOrEmpty() && TestPlan.Count == Enum.GetValues(typeof(UnitTest.TestType)).Length);

      bool testFromMainMenu = GenScene.InEntryScene;

      using UnitTestEnabler utb = new();

      results.Clear();
      results.Add($"---------- Unit Tests ----------");
      if (TestPlan[UnitTest.TestType.MainMenu])
      {
        if (Current.ProgramState != ProgramState.Entry)
        {
          GenScene.GoToMainMenu();
        }

        while (Current.ProgramState != ProgramState.Entry || LongEventHandler.AnyEventNowOrWaiting)
        {
          if (StopRequested) goto EndTest;
          yield return null;
        }

        ExecuteTests(UnitTest.TestType.MainMenu, results);
        if (StopRequested) goto EndTest;
      }

      if (TestPlan[UnitTest.TestType.GameLoaded])
      {
        LongEventHandler.QueueLongEvent(delegate ()
        {
          Root_Play.SetupForQuickTestPlay();
          PageUtility.InitGameStart();
        }, "GeneratingMap", true, ExceptionHandler, true, null);

        while (Current.ProgramState != ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting)
        {
          if (StopRequested) goto EndTest;
          yield return null;
        }

        ExecuteTests(UnitTest.TestType.GameLoaded, results);
        if (StopRequested) goto EndTest;
      }

      EndTest:;
      if (testFromMainMenu)
      {
        GenScene.GoToMainMenu();

        while (Current.ProgramState != ProgramState.Entry || LongEventHandler.AnyEventNowOrWaiting)
        {
          if (StopRequested) goto EndTest;
          yield return null;
        }
      }
      results.Add($"-------- End Unit Tests --------");
      DumpResults(results);
      Log.TryOpenLogWindow();

      static void ExceptionHandler(Exception ex)
      {
        DelayedErrorWindowRequest.Add($"Exception thrown while running tests.\n{ex}", 
          "UnitTestManager Aborted Operation");
        Scribe.ForceStop();
        GenScene.GoToMainMenu();
      }
    }

    /// <summary>
    /// Executes test suite associated with <paramref name="type"/>
    /// </summary>
    private static void ExecuteTests(UnitTest.TestType type, List<string> output)
    {
      List<string> subResults = [];
      foreach (UnitTest unitTest in unitTests[type])
      {
        Assert.IsTrue(unitTest.ExecuteOn == type);
        if (StopRequested) return;
        if (IsolatedTest != null && IsolatedTest != unitTest) continue;

        try
        {
          bool success = true;
          LongEventHandler.SetCurrentEventText($"Running {unitTest.Name}");
          foreach (UTResult result in unitTest.Execute())
          {
            if (result.Results.NullOrEmpty())
            {
              success = false;
              output.Add($"<warning>{unitTest.Name} returned test with no results.</warning>");
              continue;
            }

            foreach ((string name, bool passed) in result.Results)
            {
              string message = passed ? $"    {name} <success>Passed</success>" : $"    {name} <error>Failed</error>";
              // Dump all results for this unit test if any sub-test fails
              success &= passed;
              subResults.Add(message);
            }
          }
          if (success)
          {
            output.Add($"[{unitTest.Name}] <success>{subResults.Count} Succeeded</success>");
          }
          else
          {
            output.Add($"<error>{unitTest.Name} Failed</error>");
            output.AddRange(subResults);
          }
        }
        catch (Exception ex)
        {
          SmashLog.Message($"<error>[{unitTest.Name} Exception thrown!]</error>\n{ex}");
          output.Add($"<error>Exception thrown!</error>\n{ex}");
        }
        subResults.Clear();
      }
    }

    private static void DumpResults(List<string> results)
    {
      foreach (string result in results)
      {
        SmashLog.Message(result);
      }
    }

    private readonly struct UnitTestEnabler : IDisposable
    {
      public UnitTestEnabler()
      {
        RunningUnitTests = true;
        StopRequested = false;
      }

      void IDisposable.Dispose()
      {
        RunningUnitTests = false;
        TestPlan.Clear();
        IsolatedTest = null;
      }
    }
  }
}
