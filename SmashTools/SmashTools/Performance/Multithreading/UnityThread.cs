using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using DevTools;
using SmashTools.UnitTesting;
using UnityEngine;
using Verse;

namespace SmashTools.Performance;

[StaticConstructorOnModInit]
public class UnityThread : MonoBehaviour
{
  private readonly ConcurrentQueue<ConcurrentAction> queue = [];

  private bool keepEnabled;

  static UnityThread()
  {
    Instance = InjectToScene();
  }

  private static UnityThread Instance { get; }

  [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
    Justification = "Unity API")]
  private void Update()
  {
    if (queue.TryDequeue(out ConcurrentAction action))
    {
      action.Invoke();
    }
    else if (!UnitTestManager.RunningUnitTests && !keepEnabled)
    {
      // If there's nothing in the queue, we don't need to keep polling. Enqueueing will re-enable
      // the game object to resume execution.
      gameObject.SetActive(false);
    }
  }

  private void TryResumeExecution()
  {
    if (!queue.IsEmpty)
      gameObject.SetActive(true);
  }

  internal static void SpinUp()
  {
    Instance.keepEnabled = true;
    Instance.TryResumeExecution();
  }

  internal static void Release()
  {
    Instance.keepEnabled = false;
  }

  public static ConcurrentAction ExecuteOnMainThread(params Action[] invokeList)
  {
    if (invokeList.NullOrEmpty())
      throw new ArgumentNullException(nameof(invokeList));

    ConcurrentAction action = new(invokeList);
    Instance.queue.Enqueue(action);
    Instance.TryResumeExecution();
    return action;
  }

  /// <param name="waitTimeout">Milliseconds to wait before timout out wait handle.</param>
  /// <param name="invokeList">Actions to execute on the main thread.</param>
  public static void ExecuteOnMainThreadAndWait(int waitTimeout = 5000, params Action[] invokeList)
  {
    ConcurrentAction action = ExecuteOnMainThread(invokeList);
    bool waited = action.Wait(waitTimeout);
    Assert.IsTrue(waited, "WaitHandle timed out.");
  }

  private static UnityThread InjectToScene()
  {
    GameObject gameObject = new GameObject("UnityThread");
    UnityThread manager = gameObject.AddComponent<UnityThread>();
    DontDestroyOnLoad(gameObject);
    return manager;
  }

  public readonly struct SpinHandle : IDisposable
  {
    private static int requesters;

    public SpinHandle()
    {
      Interlocked.Increment(ref requesters);
      Instance.keepEnabled = true;
    }

    void IDisposable.Dispose()
    {
      Interlocked.Decrement(ref requesters);
      if (requesters == 0)
        Instance.keepEnabled = false;
    }
  }

  public class ConcurrentAction : IDisposable
  {
    private readonly Action[] actions;
    private readonly ManualResetEventSlim waitHandle = new();

    public ConcurrentAction(Action[] actions)
    {
      this.actions = actions;
    }

    public void Invoke()
    {
      Assert.IsTrue(UnityData.IsInMainThread);
      foreach (Action action in actions)
      {
        action();
      }
      waitHandle.Set();
    }

    public bool Wait(int waitTimeout)
    {
      Assert.IsFalse(UnityData.IsInMainThread);
      return waitHandle.Wait(waitTimeout);
    }

    void IDisposable.Dispose()
    {
      waitHandle.Dispose();
    }
  }
}