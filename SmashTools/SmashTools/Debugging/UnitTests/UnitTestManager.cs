using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Performance;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace SmashTools.Debugging;

public static class UnitTestManager
{
  private static readonly Vector2 StatusRectSize = new(240f, 75f);

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

    UnitTestAsyncHandler.Init();
  }

  private static Dictionary<UnitTest.TestType, bool> TestTypes { get; } = [];

  private static TestPlanDef TestPlan { get; set; }

  private static UnitTest IsolatedTest { get; set; }

  public static List<UnitTest> AllUnitTests => unitTests.Values.SelectMany(t => t).ToList();

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

  /// <remarks>Not including any test types will default to running all available tests.</remarks>
  private static void ExecuteUnitTests(params UnitTest.TestType[] testTypes)
  {
    if (testTypes.NullOrEmpty())
      testTypes = [UnitTest.TestType.MainMenu, UnitTest.TestType.GameLoaded];

    EnableForTests(testTypes);

    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineManager.QueueInvoke(UnitTestRoutine);
    });
  }

  private static void ExecuteUnitTests(TestPlanDef testPlanDef)
  {
    TestPlan = testPlanDef;
    LongEventHandler.QueueLongEvent(TestPlanRoutine, null, true, TestExceptionHandler,
      showExtraUIInfo: false);
  }

  private static void TestPlanRoutine()
  {
    using UnitTestEnabler ute = new();

    // Force enable MonoBehaviour so we can process MainThread invokes immediately.
    using UnityThread.SpinHandle handle = new();

    results.Clear();
    results.Add("---------- Unit Tests ----------");

    UnitTest.TestType currentTestType = UnitTest.TestType.Disabled;

    foreach (TestBlock block in TestPlan.plan)
    {
      if (StopRequested)
        goto EndTest;
      if (block.type == UnitTest.TestType.Disabled)
        continue;

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
            break;
          }
          case UnitTest.TestType.GameLoaded:
          {
            if (!UnitTestAsyncHandler.GenerateMap(block))
              goto EndTest;
            break;
          }
          case UnitTest.TestType.Disabled:
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
    results.Add("-------- End Unit Tests --------");
    DumpResults(results);
    UnityThread.ExecuteOnMainThread(Log.TryOpenLogWindow);
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
        if (StopRequested)
          goto EndTest;
        yield return null;
      }

      LongEventHandler.QueueLongEvent(
        delegate { ExecuteTests(UnitTest.TestType.MainMenu, results); }, null, true,
        TestExceptionHandler);

      while (LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested) goto EndTest;
        yield return null;
      }

      if (StopRequested) goto EndTest;
    }

    if (TestTypes[UnitTest.TestType.GameLoaded])
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

      LongEventHandler.QueueLongEvent(
        delegate { ExecuteTests(UnitTest.TestType.GameLoaded, results); }, null, true,
        TestExceptionHandler);
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
      if (StopRequested) return;
      if (IsolatedTest != null && IsolatedTest != unitTest) continue;

      Log.Message($"Running {unitTest.Name}...");
      Assert.IsTrue(unitTest.ExecuteOn == type,
        $"Executing unit test {unitTest.Name} on wrong TestType ({unitTest.ExecuteOn} on {type})");

      try
      {
        bool success = true;
        LongEventHandler.SetCurrentEventText($"Running {unitTest.Name}");
        foreach (UTResult summary in unitTest.Execute())
        {
          if (summary.Results.NullOrEmpty())
          {
            success = false;
            output.Add($"<warning>{unitTest.Name} returned test with no results.</warning>");
            continue;
          }

          foreach ((string name, UTResult.Result result) in summary.Results)
          {
            string message = result switch
            {
              UTResult.Result.Failed  => $"    {name} <error>Failed</error>",
              UTResult.Result.Passed  => $"    {name} <success>Passed</success>",
              UTResult.Result.Skipped => $"    {name} Skipped",
              _                       => throw new NotImplementedException(),
            };

            // Dump all results for this unit test if any sub-test fails
            success &= result != UTResult.Result.Failed;
            subResults.Add(message);
          }
        }

        if (success)
        {
          output.Add($"[{unitTest.Name}] <success>{subResults.Count} Succeeded</success>");
        }
        else
        {
          output.Add($"[{unitTest.Name}] <error>Failed</error>");
          output.AddRange(subResults);
        }
      }
      catch (Exception ex)
      {
        output.Add($"[{unitTest.Name}] <error>Exception thrown!</error>\n{ex}");
      }

      subResults.Clear();
    }
  }

  private static void DumpResults(List<string> resultsToDump)
  {
    foreach (string result in resultsToDump)
    {
      SmashLog.Message(result);
    }
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