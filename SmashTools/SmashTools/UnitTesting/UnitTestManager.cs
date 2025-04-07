//#define RUN_ASYNC

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DevTools;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Performance;
using UnityEngine;
using Verse;
using Verse.Profile;
using Verse.Sound;
using Result = SmashTools.UnitTesting.UTResult.Result;

namespace SmashTools.UnitTesting;

/// <summary>
/// Test Manager for running unit tests in RimWorld. Tests can be ran in isolation or be executed
/// as part of a test suite. This manager will handle switching between scenes and consolidating
/// test results in an explorer widget, allowing you to view each test class and its results.
/// <para/>
/// Due to Unity being single threaded, tests are run synchronously. This will block the
/// main thread and cause the application to hang for the duration of test execution. Because
/// RimWorld is so tightly coupled it's impossible to predict where it might call to Unity's API.
/// </summary>
public static class UnitTestManager
{
  private static bool runningUnitTests;
  private static readonly Dictionary<TestType, List<UnitTest>> unitTests = [];

  /// <summary>
  /// Event for UnitTest state changes.
  /// <para/>
  /// This event will fire when unit testing begins, and again when it finishes.
  /// </summary>
  public static event Action<bool> OnUnitTestStateChange;

  static UnitTestManager()
  {
    ConcurrentDictionary<TestType, ConcurrentBag<UnitTest>> tests = [];
    foreach (TestType testType in Enum.GetValues(typeof(TestType)))
    {
      tests.TryAdd(testType, []);
    }

    foreach (Type type in typeof(UnitTest).AllSubclassesNonAbstract())
    {
      if (!type.IsAbstract)
      {
        UnitTest unitTest = (UnitTest)Activator.CreateInstance(type);
        tests[unitTest.ExecuteOn].Add(unitTest);
      }
    }

    foreach (TestType testType in Enum.GetValues(typeof(TestType)))
    {
      unitTests.Add(testType,
        [.. tests[testType].OrderByDescending(test => (int)test.Priority)]);
    }
#if RUN_ASYNC
    UnitTestAsyncHandler.Init();
#endif
  }

  private static Dictionary<TestType, bool> TestTypes { get; } = [];

  private static TestSuiteDef TestPlan { get; set; }

  private static UnitTest IsolatedTest { get; set; }

  internal static bool StopRequested { get; private set; }

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

  private static void ShowResults(List<TestBatch> results)
  {
    if (results.NullOrEmpty())
    {
      Log.Error("No test results to show.");
      return;
    }
    Find.WindowStack.Add(new Dialog_TestExplorer(results));
  }

  public static void ShowMenu()
  {
    SoundDefOf.Click.PlayOneShotOnCamera();
    List<Toggle> toggles = [];

    foreach (UnitTest test in unitTests.Values.SelectMany(list => list)
     .OrderBy(test => test.ExecuteOn)
     .ThenBy(test => test.Name))
    {
      TestType testType = test.ExecuteOn;
      if (testType == TestType.Disabled) continue;

      Toggle toggle = new(test.Name, TestTypeLabel(testType), () => false,
        _ => { },
        onToggle: delegate
        {
          Run(test);
          Find.WindowStack.WindowOfType<Dialog_ActionList>()?.Close();
        });
      toggles.Add(toggle);
    }

    foreach (TestSuiteDef testPlanDef in DefDatabase<TestSuiteDef>.AllDefsListForReading)
    {
      Toggle toggle = new(testPlanDef.LabelCap, "Test Plan", () => false, _ => { },
        onToggle: delegate
        {
          RunPlan(testPlanDef);
          Find.WindowStack.WindowOfType<Dialog_ActionList>()?.Close();
        });
      toggles.Add(toggle);
    }

    Find.WindowStack.Add(new Dialog_ActionList("Unit Tests", toggles));
  }

  [UsedImplicitly]
  public static void Run(UnitTest unitTest)
  {
    if (unitTest.ExecuteOn == TestType.Disabled)
      return;

    IsolatedTest = unitTest;
    ExecuteUnitTests(unitTest.ExecuteOn);
  }

  [UsedImplicitly]
  public static void RunPlan(TestSuiteDef suiteDef)
  {
    ExecuteUnitTests(suiteDef);
  }

  private static void EnableForTests(params TestType[] types)
  {
    TestTypes.Clear();
    foreach (TestType testType in Enum.GetValues(typeof(TestType)))
    {
      TestTypes[testType] = types.Contains(testType);
    }
  }

  /// <remarks>Not including any test types will default to running all available tests.</remarks>
  private static void ExecuteUnitTests(params TestType[] testTypes)
  {
    if (testTypes.NullOrEmpty())
      testTypes = [TestType.MainMenu, TestType.Playing];

    EnableForTests(testTypes);

    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineManager.QueueInvoke(UnitTestRoutine);
    });
  }

  private static void ExecuteUnitTests(TestSuiteDef suiteDef)
  {
    TestPlan = suiteDef;
#if RUN_ASYNC
    LongEventHandler.QueueLongEvent(TestPlanAsync, null, true, TestExceptionHandler,
      showExtraUIInfo: false);
#else
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineManager.QueueInvoke(TestPlanRoutine);
    });
#endif
  }

  private static IEnumerator TestPlanRoutine()
  {
    using UnitTestEnabler ute = new();

    // Force enable MonoBehaviour so we can process MainThread invokes immediately.
    using UnityThread.SpinHandle handle = new();

    List<TestBatch> results = [];
    TestType currentTestType = TestType.Disabled;
    foreach (TestBlock block in TestPlan.plan)
    {
      if (StopRequested)
        goto EndTest;
      if (block.type == TestType.Disabled)
        continue;

      if (currentTestType != block.type)
      {
        // Transition between scenes
        currentTestType = block.type;
        switch (currentTestType)
        {
          case TestType.MainMenu:
          {
            if (Current.ProgramState != ProgramState.Entry)
            {
              GenScene.GoToMainMenu();
              while (Current.ProgramState != ProgramState.Entry ||
                LongEventHandler.AnyEventNowOrWaiting)
              {
                if (StopRequested)
                  goto EndTest;
                yield return null;
              }
            }
            break;
          }
          case TestType.Playing:
          {
            if (!UnitTestAsyncHandler.GenerateMap(block))
              goto EndTest;

            while (Current.ProgramState != ProgramState.Playing ||
              LongEventHandler.AnyEventNowOrWaiting)
            {
              if (StopRequested)
                goto EndTest;
              yield return null;
            }
            yield return new WaitForSecondsRealtime(1);
            break;
          }
          case TestType.Disabled:
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
      while (Current.ProgramState != ProgramState.Entry ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested)
          goto EndTest;
        yield return null;
      }
    }
    LongEventHandler.ClearQueuedEvents();
    ShowResults(results);
  }

  private static void TestPlanAsync()
  {
    using UnitTestEnabler ute = new();

    // Force enable MonoBehaviour so we can process MainThread invokes immediately.
    using UnityThread.SpinHandle handle = new();

    List<TestBatch> results = [];
    TestType currentTestType = TestType.Disabled;
    foreach (TestBlock block in TestPlan.plan)
    {
      if (StopRequested)
        goto EndTest;
      if (block.type == TestType.Disabled)
        continue;

      if (currentTestType != block.type)
      {
        // Transition between scenes
        currentTestType = block.type;
        switch (currentTestType)
        {
          case TestType.MainMenu:
          {
            if (Current.ProgramState != ProgramState.Entry)
            {
              GenScene.GoToMainMenu();
            }
            break;
          }
          case TestType.Playing:
          {
            if (!UnitTestAsyncHandler.GenerateMap(block))
              goto EndTest;
            break;
          }
          case TestType.Disabled:
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
    }
    LongEventHandler.ClearQueuedEvents();
    ShowResults(results);
  }

  private static IEnumerator UnitTestRoutine()
  {
    Assert.IsTrue(!TestTypes.NullOrEmpty() &&
      TestTypes.Count == Enum.GetValues(typeof(TestType)).Length);

    bool testFromMainMenu = GenScene.InEntryScene;

    using UnitTestEnabler utb = new();

    List<TestBatch> results = [];
    if (TestTypes[TestType.MainMenu])
    {
      if (Current.ProgramState != ProgramState.Entry)
      {
        GenScene.GoToMainMenu();
      }

      while (Current.ProgramState != ProgramState.Entry || LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested)
          goto EndTest;
        yield return null;
      }

      ExecuteTests(TestType.MainMenu, results);

      while (LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested) goto EndTest;
        yield return null;
      }

      if (StopRequested) goto EndTest;
    }

    if (TestTypes[TestType.Playing])
    {
      LongEventHandler.QueueLongEvent(delegate
      {
        MemoryUtility.ClearAllMapsAndWorld();
        Root_Play.SetupForQuickTestPlay();
        PageUtility.InitGameStart();
      }, "GeneratingMap", true, TestExceptionHandler);

      while (Current.ProgramState != ProgramState.Playing ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested) goto EndTest;
        yield return null;
      }

      ExecuteTests(TestType.Playing, results);
    }

    EndTest: ;
    if (testFromMainMenu)
    {
      GenScene.GoToMainMenu();

      while (Current.ProgramState != ProgramState.Entry ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested)
          break;
        yield return null;
      }
    }
    ShowResults(results);
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
  private static void ExecuteTests(TestType type, List<TestBatch> output)
  {
    ExecuteTests(type, unitTests[type], output);
  }

  private static void ExecuteTests(TestType type, List<UnitTest> tests,
    List<TestBatch> output)
  {
    foreach (UnitTest unitTest in tests)
    {
      if (StopRequested)
        return;
      if (IsolatedTest != null && IsolatedTest != unitTest)
        continue;

      Assert.IsTrue(unitTest.ExecuteOn == type,
        $"Executing unit test {unitTest.Name} on wrong TestType ({unitTest.ExecuteOn} on {type})");

      TestBatch batch = new(unitTest);
      using Assert.ThrowOnAssertEnabler te = new();
      // Set Up
      try
      {
        unitTest.SetUp();
      }
      catch (AssertFailException ex)
      {
        batch.FailWithMessage($"[SetUp] <error>Assertion failed!</error>\n{ex}");
        continue;
      }
      catch (Exception ex)
      {
        batch.FailWithMessage($"[SetUp] <error>Exception thrown!</error>\n{ex}");
        continue;
      }

      // Execute
      try
      {
        foreach (UTResult resultGroup in unitTest.Execute())
        {
          if (resultGroup.Tests.NullOrEmpty())
          {
            batch.Add(UTResult.For($"{unitTest.Name} returned test with no results.",
              Result.Skipped));
            continue;
          }

          batch.Add(resultGroup);
        }
      }
      catch (AssertFailException ex)
      {
        batch.FailWithMessage($"<error>Assertion failed!</error>\n{ex}");
      }
      catch (Exception ex)
      {
        batch.FailWithMessage($"<error>Exception thrown!</error>\n{ex}");
      }

      // Clean Up
      try
      {
        unitTest.CleanUp();
      }
      catch (AssertFailException ex)
      {
        batch.FailWithMessage($"<error>Assertion failed!</error>\n{ex}");
      }
      catch (Exception ex)
      {
        batch.FailWithMessage($"<error>Exception thrown!</error>\n{ex}");
      }
      output.Add(batch);
    }
  }

  private static string TestTypeLabel(TestType testType)
  {
    return testType switch
    {
      TestType.Disabled => "Disabled",
      TestType.MainMenu => "Main Menu",
      TestType.Playing  => "Playing",
      _                 => throw new NotImplementedException(),
    };
  }

  /// <summary>
  /// Intercept initial map generation when running unit tests to generate map from template.
  /// </summary>
  /// <notes>
  /// Postfix | Game::InitNewGame
  /// </notes>
  internal static bool InitNewGame(Game __instance, List<Map> ___maps)
  {
    if (RunningUnitTests)
    {
      string modManifest = LoadedModManager.RunningMods.Select(mod =>
          $"{mod.PackageIdPlayerFacing}" +
          $"{(mod.ModMetaData.VersionCompatible ? "(incompatible version)" : "")}")
       .ToLineList("  - ");
      Log.Message($"Initializing new game with mods:\n{modManifest}");
      if (___maps.Any())
      {
        Log.Error("Called InitNewGame() but there already is a map. There should be 0 maps...");
        return false;
      }

      if (__instance.InitData == null)
      {
        Log.Error("Called InitNewGame() but init data is null. Create it first.");
        return false;
      }

      MemoryUtility.UnloadUnusedUnityAssets();
      Current.ProgramState = ProgramState.MapInitializing;
      IntVec3 intVec = new(__instance.InitData.mapSize, 1, __instance.InitData.mapSize);
      Settlement settlement =
        Find.WorldObjects.Settlements.Find(stl => stl.Faction == Faction.OfPlayer);

      if (settlement == null)
      {
        throw new InvalidOperationException(
          "Could not generate starting map because there is no any player faction base.");
      }

      __instance.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;
      __instance.Info.startingTile = __instance.InitData.startingTile;
      __instance.Info.startingAndOptionalPawns = __instance.InitData.startingAndOptionalPawns;
      Map currentMap = MapGenerator.GenerateMap(intVec, settlement, settlement.MapGeneratorDef,
        settlement.ExtraGenStepDefs);
      __instance.World.info.initialMapSize = intVec;
      if (__instance.InitData.permadeath)
      {
        __instance.Info.permadeathMode = true;
        __instance.Info.permadeathModeUniqueName =
          PermadeathModeUtility.GeneratePermadeathSaveName();
      }

      PawnUtility.GiveAllStartingPlayerPawnsThought(ThoughtDefOf.NewColonyOptimism);
      __instance.FinalizeInit();
      Current.Game.CurrentMap = currentMap;
      Find.CameraDriver.JumpToCurrentMapLoc(MapGenerator.PlayerStartSpot);
      Find.CameraDriver.ResetSize();

      // Don't bother with RimWorld's "pause on load" logic, we won't be staying
      // in this map past execution of unit tests. All we need is 1 tick.
      LongEventHandler.ExecuteWhenFinished(__instance.tickManager.DoSingleTick);

      Find.Scenario.PostGameStart();
      __instance.history.FinalizeInit();
      ResearchUtility.ApplyPlayerStartingResearch();
      GameComponentUtility.StartedNewGame();
      __instance.InitData = null;

      return false;
    }

    return true;
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