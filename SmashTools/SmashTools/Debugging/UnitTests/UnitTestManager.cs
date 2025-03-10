using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SmashTools.Debugging;

public static class UnitTestManager
{
  private static bool runningUnitTests;
  private static readonly Dictionary<UnitTest.TestType, List<UnitTest>> unitTests = [];
  private static readonly List<string> results = [];

  public static event Action<bool> OnUnitTestStateChange;

  static UnitTestManager()
  {
    ConcurrentDictionary<UnitTest.TestType, ConcurrentBag<UnitTest>> tests = [];
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
      unitTests.Add(testType,
        [.. tests[testType].OrderByDescending(test => (int)test.Priority)]);
    }
  }

  private static Dictionary<UnitTest.TestType, bool> TestTypes { get; set; } = [];

  private static TestPlanDef TestPlan { get; set; }

  private static UnitTest IsolatedTest { get; set; }

  public static List<UnitTest> AllUnitTests => unitTests.Values.SelectMany(t => t).ToList();

  private static bool StopRequested { get; set; }

  // Supresses startup action unit tests once it has been executed once to avoid infinite test loops when transitioning scenes.
  private static bool UnitTestsExecuted { get; set; }

  public static bool RunningUnitTests
  {
    get { return runningUnitTests; }
    private set
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

  public static void RunPlan(TestPlanDef planDef)
  {
    ExecuteUnitTests(planDef);
  }

  private static void EnableForTests(params UnitTest.TestType[] types)
  {
    TestTypes.Clear();
    foreach (UnitTest.TestType testType in Enum.GetValues(typeof(UnitTest.TestType)))
    {
      TestTypes[testType] = types.Contains(testType);
    }
  }

  private static void StopHugslibQuickstart()
  {
    if (ModsConfig.IsActive("UnlimitedHugs.HugsLib"))
    {
      Type type = AccessTools.TypeByName("HugsLib.Quickstart.QuickstartController");
      Assert.IsNotNull(type);
      MethodInfo abortMethod = AccessTools.Method(type, "StatusBoxAbortRequestedHandler");
      Assert.IsNotNull(abortMethod);
      abortMethod.Invoke(null, [false]);
    }
  }

  /// <remarks>Not including any test types will default to running all available tests.</remarks>
  private static void ExecuteUnitTests(params UnitTest.TestType[] testTypes)
  {
    if (testTypes.NullOrEmpty())
      testTypes = [UnitTest.TestType.MainMenu, UnitTest.TestType.GameLoaded];

    UnitTestsExecuted = true;
    EnableForTests(testTypes);

    StopHugslibQuickstart();
    LongEventHandler.ExecuteWhenFinished(delegate()
    {
      CoroutineManager.QueueInvoke(UnitTestRoutine);
    });
  }

  private static void ExecuteUnitTests(TestPlanDef testPlanDef)
  {
    UnitTestsExecuted = true;
    TestPlan = testPlanDef;
    StopHugslibQuickstart();
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineManager.QueueInvoke(TestPlanRoutine);
    });
  }
#endif

  private static IEnumerator TestPlanRoutine()
  {
    using UnitTestEnabler utb = new();

    results.Clear();
    results.Add($"---------- Unit Tests ----------");

    UnitTest.TestType currentTestType = UnitTest.TestType.Disabled;
    foreach (TestBlock block in TestPlan.plan)
    {
      if (StopRequested) break;
      if (block.type == UnitTest.TestType.Disabled) continue;

      if (currentTestType != block.type)
      {
        // Transition between scenes
        currentTestType = block.type;
        switch (currentTestType)
        {
          case UnitTest.TestType.MainMenu:
          {
            if (Current.ProgramState != ProgramState.Entry)
            {
              GenScene.GoToMainMenu();
            }

            while (Current.ProgramState != ProgramState.Entry ||
                   LongEventHandler.AnyEventNowOrWaiting)
            {
              if (StopRequested) goto EndTest;
              yield return null;
            }

            break;
          }
          case UnitTest.TestType.GameLoaded:
          {
            LongEventHandler.QueueLongEvent(delegate()
            {
              Root_Play.SetupForQuickTestPlay();
              PageUtility.InitGameStart();
            }, "GeneratingMap", true, TestExceptionHandler, true, null);

            while (Current.ProgramState != ProgramState.Playing ||
                   LongEventHandler.AnyEventNowOrWaiting)
            {
              if (StopRequested) goto EndTest;
              yield return null;
            }

            break;
          }
          default:
            throw new NotImplementedException();
        }
      }

      ExecuteTests(block.type, block.UnitTests, results);
    }

    EndTest: ;
    if (Current.ProgramState != ProgramState.Entry)
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
  }

  private static IEnumerator UnitTestRoutine()
  {
    Assert.IsTrue(!TestTypes.NullOrEmpty() &&
                  TestTypes.Count == Enum.GetValues(typeof(UnitTest.TestType)).Length);

    bool testFromMainMenu = GenScene.InEntryScene;

    using UnitTestEnabler utb = new();

    results.Clear();
    results.Add($"---------- Unit Tests ----------");
    if (TestTypes[UnitTest.TestType.MainMenu])
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

    if (TestTypes[UnitTest.TestType.GameLoaded])
    {
      LongEventHandler.QueueLongEvent(delegate()
      {
        Root_Play.SetupForQuickTestPlay();
        PageUtility.InitGameStart();
      }, "GeneratingMap", true, TestExceptionHandler, true, null);

      while (Current.ProgramState != ProgramState.Playing ||
             LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested) goto EndTest;
        yield return null;
      }

      ExecuteTests(UnitTest.TestType.GameLoaded, results);
      if (StopRequested) goto EndTest;
    }

    EndTest: ;
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
  }

  private static void TestExceptionHandler(Exception ex)
  {
    DelayedErrorWindowRequest.Add($"Exception thrown while running tests.\n{ex}",
      "UnitTestManager Aborted Operation");
    Scribe.ForceStop();
    GenScene.GoToMainMenu();
  }

  /// <summary>
  /// Executes test suite associated with <paramref name="type"/>
  /// </summary>
  private static void ExecuteTests(UnitTest.TestType type, List<string> output)
  {
    ExecuteTests(type, unitTests[type], output);
  }

  private static void ExecuteTests(UnitTest.TestType type, List<UnitTest> tests,
    List<string> output)
  {
    List<string> subResults = [];
    foreach (UnitTest unitTest in tests)
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
            string message = passed ?
                               $"    {name} <success>Passed</success>" :
                               $"    {name} <error>Failed</error>";
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
      TestTypes.Clear();
      IsolatedTest = null;
      TestPlan = null;
    }
  }
}