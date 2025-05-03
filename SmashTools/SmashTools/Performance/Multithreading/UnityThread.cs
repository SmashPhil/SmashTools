using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Performance;

[StaticConstructorOnStartup]
public class UnityThread : MonoBehaviour
{
  private static SynchronizationContext mainContext;

  private readonly List<OnUpdate> onUpdateMethods = [];

  /// <returns>
  /// <see langword="true"/> if <see cref="OnUpdate"/> should remain in queue for the next frame.
  /// <see langword="false"/> if it should be dequeued immediately.
  /// </returns>
  public delegate bool OnUpdate();

  static UnityThread()
  {
    Instance = InjectToScene();
  }

  private static UnityThread Instance { get; }

  [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
    Justification = "Unity API")]
  private void Awake()
  {
    mainContext = SynchronizationContext.Current;
  }

  [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
    Justification = "Unity API")]
  private void Update()
  {
    for (int i = onUpdateMethods.Count - 1; i >= 0; i--)
    {
      if (!onUpdateMethods[i]())
        onUpdateMethods.RemoveAt(i);
    }
  }

  public static void StartUpdate(OnUpdate onUpdate)
  {
    if (!UnityData.IsInMainThread)
    {
      Trace.Fail(
        "Trying to add update method to queue from another thread. This can only be done from the main thread.");
      return;
    }
    Instance.onUpdateMethods.Add(onUpdate);
  }

  public static void ExecuteOnMainThread(params Action[] invokeList)
  {
    if (invokeList.NullOrEmpty())
      throw new ArgumentNullException(nameof(invokeList));

    if (UnityData.IsInMainThread)
    {
      foreach (Action action in invokeList)
        action();
      return;
    }
    ConcurrentAction concurrentAction = new(invokeList);
    mainContext.Post(concurrentAction.InvokeAndDispose, null);
  }

  /// <param name="waitTimeout">Milliseconds to wait before timout out wait handle.</param>
  /// <param name="invokeList">Actions to execute on the main thread.</param>
  public static void ExecuteOnMainThreadAndWait(int waitTimeout = 5000, params Action[] invokeList)
  {
    if (invokeList.NullOrEmpty())
      throw new ArgumentNullException(nameof(invokeList));

    if (UnityData.IsInMainThread)
    {
      foreach (Action action in invokeList)
        action();
      return;
    }
    using ConcurrentAction concurrentAction = new(invokeList);
    bool waited = concurrentAction.Wait(waitTimeout);
    Assert.IsTrue(waited, "WaitHandle timed out.");
  }

  private static UnityThread InjectToScene()
  {
    GameObject gameObject = new("UnityThread");
    UnityThread manager = gameObject.AddComponent<UnityThread>();
    DontDestroyOnLoad(gameObject);
    return manager;
  }

  private class ConcurrentAction : IDisposable
  {
    private readonly Action[] actions;
    private readonly ManualResetEventSlim waitHandle = new();

    private bool waitedOn;

    public ConcurrentAction(Action[] actions)
    {
      this.actions = actions;
    }

    public void Invoke(object state)
    {
      Assert.IsTrue(UnityData.IsInMainThread);
      foreach (Action action in actions)
      {
        action();
      }
      waitHandle.Set();
    }

    /// <summary>
    /// Only used for 'fire and forget' method invokes, there should be no
    /// threads or processes waiting on this waitHandle
    /// </summary>
    /// <param name="state"></param>
    public void InvokeAndDispose(object state)
    {
      Assert.IsFalse(waitedOn);
      Assert.IsTrue(UnityData.IsInMainThread);
      foreach (Action action in actions)
      {
        action();
      }
      waitHandle.Dispose();
    }

    public bool Wait(int waitTimeout)
    {
      Assert.IsFalse(UnityData.IsInMainThread);
      waitedOn = true;
      bool waited = waitHandle.Wait(waitTimeout);
      Dispose();
      return waited;
    }

    public void Dispose()
    {
      waitHandle.Dispose();
    }
  }
}