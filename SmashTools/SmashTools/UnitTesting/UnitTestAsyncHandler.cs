using System;
using System.Collections.Concurrent;
using System.Reflection;
using DevTools;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;
using Verse.Profile;

namespace SmashTools.UnitTesting;

/*
 * RimWorld handles async scene loading by queueing a long event and checking the levelToLoad key
 * for determining which scene to load. Because of this, we can't properly transition scenes while
 * running unit tests in a long event.
 *
 * The unit tests are not utilizing any long events so we just need a synchronous way to transition
 * scenes while we're still in the middle of the long event for running all unit tests.
 *
 * NOTE - These util methods should not be used outside of this context.
 */
internal static class UnitTestAsyncHandler
{
  private static Action ExecuteToExecuteWhenFinished;

  private static readonly ConcurrentQueue<LongEventAction> longEventQueue = [];

  private static bool SceneLoadedStartInvoked { get; set; }

  internal static void Init()
  {
    MethodInfo executeMethod =
      AccessTools.Method(typeof(LongEventHandler), "ExecuteToExecuteWhenFinished");
    ExecuteToExecuteWhenFinished =
      (Action)Delegate.CreateDelegate(typeof(Action), executeMethod);
    Assert.IsNotNull(ExecuteToExecuteWhenFinished);

    ProjectSetup.Harmony.Patch(AccessTools.Method(typeof(Root_Entry), nameof(Root_Entry.Start)),
      postfix: new HarmonyMethod(typeof(UnitTestAsyncHandler), nameof(SignalReadyToContinue)));
    ProjectSetup.Harmony.Patch(AccessTools.Method(typeof(Root_Play), nameof(Root_Play.Start)),
      postfix: new HarmonyMethod(typeof(UnitTestAsyncHandler), nameof(SignalReadyToContinue)));
    ProjectSetup.Harmony.Patch(
      AccessTools.Method(typeof(LongEventHandler), nameof(LongEventHandler.ExecuteWhenFinished)),
      prefix: new HarmonyMethod(typeof(UnitTestAsyncHandler), nameof(InterceptMainThreadInvokes)));
    ProjectSetup.Harmony.Patch(
      AccessTools.Method(typeof(LongEventHandler), nameof(LongEventHandler.QueueLongEvent),
        parameters:
        [
          typeof(Action), typeof(string), typeof(bool), typeof(Action<Exception>), typeof(bool),
          typeof(Action)
        ]),
      prefix: new HarmonyMethod(typeof(UnitTestAsyncHandler), nameof(InterceptLongEvent)));
    ProjectSetup.Harmony.Patch(
      AccessTools.Method(typeof(LongEventHandler), nameof(LongEventHandler.QueueLongEvent),
        parameters:
        [
          typeof(Action), typeof(string), typeof(string), typeof(bool), typeof(Action<Exception>),
          typeof(bool)
        ]),
      prefix: new HarmonyMethod(typeof(UnitTestAsyncHandler), nameof(InterceptSceneChange)));
    ProjectSetup.Harmony.Patch(
      AccessTools.Method(typeof(ScreenFader), nameof(ScreenFader.StartFade),
        parameters: [typeof(Color), typeof(float), typeof(float)]),
      prefix: new HarmonyMethod(typeof(UnitTestAsyncHandler), nameof(BlockScreenFades)));
  }

#region Patches

  private static void SignalReadyToContinue()
  {
    SceneLoadedStartInvoked = true;
  }

  private static bool InterceptMainThreadInvokes(Action action)
  {
    if (UnitTestManager.RunningUnitTests)
    {
      if (UnityData.IsInMainThread)
      {
        action();
        return false;
      }
      // Execute actions immediately on the main thread since
      // we won't be terminating this long event anytime soon.
      UnityThread.ExecuteOnMainThreadAndWait(invokeList: action);
      return false;
    }
    return true;
  }

  private static bool InterceptLongEvent(Action action, string textKey,
    Action<Exception> exceptionHandler, Action callback)
  {
    if (UnitTestManager.RunningUnitTests)
    {
      LongEventAction longEvent = new(action, textKey, exceptionHandler, callback);
      if (UnityData.IsInMainThread)
      {
        Assert.IsFalse(SceneLoadedStartInvoked);
        longEventQueue.Enqueue(longEvent);
      }
      else
      {
        longEvent.Invoke();
      }
      return false;
    }
    return true;
  }

  private static bool InterceptSceneChange(Action preLoadLevelAction, string levelToLoad,
    string textKey, Action<Exception> exceptionHandler)
  {
    if (UnitTestManager.RunningUnitTests)
    {
      try
      {
        // Scene transitions should be purely driven by the unit test long event
        Assert.IsFalse(UnityData.IsInMainThread);
        if (!textKey.NullOrEmpty())
          LongEventHandler.SetCurrentEventText(textKey.Translate());
        preLoadLevelAction();
        SceneLoadedStartInvoked = false;
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(levelToLoad);
        while (!asyncOp.isDone || !SceneLoadedStartInvoked)
        {
          if (UnitTestManager.StopRequested)
            return false;
        }
        ExecuteLongEventQueue();
      }
      catch (Exception ex)
      {
        // If exception isn't handled just rethrow
        if (exceptionHandler != null)
          exceptionHandler(ex);
        else
          throw;
      }
      return false;
    }
    return true;
  }

#endregion Patches

  // RimWorld initializes various long events in MonoBehaviour Start methods. We still want them
  // to execute in our long event so while the scene is loading
  private static void ExecuteLongEventQueue()
  {
    while (longEventQueue.TryDequeue(out LongEventAction action))
    {
      action.Invoke();
    }
  }

  internal static bool GenerateMap(TestBlock block)
  {
    MemoryUtility.ClearAllMapsAndWorld();
    SetupForTest(block.template);
    PageUtility.InitGameStart();
    return true;
  }

  private static void SetupForTest(GenerationTemplate template = null)
  {
    // If template is null, default to QuickTest parameters
    if (template == null)
    {
      Root_Play.SetupForQuickTestPlay();
      return;
    }

    Current.ProgramState = ProgramState.Entry;
    Current.Game = new Game();
    Current.Game.InitData = new GameInitData();
    Current.Game.Scenario = TemplateScenarioDefOf.TestScenario.scenario;
    Find.Scenario.PreConfigure();
    Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);

    Current.Game.World = WorldGenerator.GenerateWorld(template.world.percent,
      GenText.RandomSeedString(),
      template.world.rainfall, template.world.temperature, template.world.population);
    Find.GameInitData.ChooseRandomStartingTile();
    if (template.map?.biome != null)
    {
      Find.WorldGrid[Find.GameInitData.startingTile].biome = template.map.biome;
    }

    Find.Scenario.PostIdeoChosen();
  }

  private static bool BlockScreenFades()
  {
    // If unit tests are running, block all screen fader actions.
    if (UnitTestManager.RunningUnitTests)
    {
      ScreenFader.SetColor(Color.clear);
      return false;
    }
    return true;
  }

  public static void StopHugslibQuickstart()
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

  private class LongEventAction
  {
    private readonly Action action;
    private readonly string textKey;
    private readonly Action<Exception> exceptionHandler;
    private readonly Action callback;

    public LongEventAction(Action action, string textKey, Action<Exception> exceptionHandler,
      Action callback)
    {
      this.action = action;
      this.textKey = textKey;
      this.exceptionHandler = exceptionHandler;
      this.callback = callback;
    }

    public void Invoke()
    {
      try
      {
        if (!textKey.NullOrEmpty())
          LongEventHandler.SetCurrentEventText(textKey.Translate());
        action();
        callback?.Invoke();
      }
      catch (Exception ex)
      {
        // If exception isn't handled just rethrow
        if (exceptionHandler != null)
          exceptionHandler(ex);
        else
          throw;
      }
    }
  }
}