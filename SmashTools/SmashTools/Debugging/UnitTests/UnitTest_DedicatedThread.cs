using System;
using System.Collections.Generic;
using System.Threading;
using DevTools;
using DevTools.UnitTesting;
using SmashTools.Performance;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
internal class UnitTest_DedicatedThread
{
  private const int ThreadJoinTimeout = 5000;
  private const int WaitTime = 1000;
  private const int ItemWorkMS = WaitTime / 10;

  private void Dispatcher()
  {
    DedicatedThread dedicatedThread = ThreadManager.CreateNew();
    // Should never start suspended
    Assert.IsFalse(dedicatedThread.IsSuspended);

    ManualResetEventSlim mres = new(false);

    // No signal should be received, it should've already entered a blocked state while waiting 
    // for an item to enqueue.
    Expect.IsTrue("No Polling", dedicatedThread.IsBlocked);

    AsyncLongOperationAction pollingOp = AsyncPool<AsyncLongOperationAction>.Get();
    pollingOp.OnInvoke += () => SleepThread(ItemWorkMS, mres: mres);
    dedicatedThread.Enqueue(pollingOp);
    // Signal should be received this time, enqueueing item will set the event handler and resume
    // the thread's execution.
    Expect.IsFalse("Execution Resumed", dedicatedThread.IsBlocked);

    Expect.IsTrue("WaitHandle Execution", mres.Wait(TimeSpan.FromMilliseconds(WaitTime)));
    mres.Reset();

    Assert.IsTrue(dedicatedThread.QueueCount == 0);
    Expect.IsTrue("Execution Waiting", dedicatedThread.IsBlocked);

    EnqueueWorkItems(dedicatedThread, mres);
    Assert.IsTrue(dedicatedThread.QueueCount > 0);
    Assert.IsTrue(dedicatedThread.IsBlocked);

    // Stop will send an event to the wait handle to resume so that it may exit
    dedicatedThread.Stop();
    // Allow WaitTime limit for each item in queue, but it should take nowhere near this long.
    Expect.IsTrue("WaitHandle Stop Gracefully",
      dedicatedThread.thread.Join(TimeSpan.FromMilliseconds(ThreadJoinTimeout)));
    mres.Reset();

    Expect.IsTrue("Stop Gracefully",
      dedicatedThread.QueueCount == 0 && dedicatedThread.Terminated);
    dedicatedThread.Release();

    // Start a new thread so we can check immediate stop
    dedicatedThread = ThreadManager.CreateNew();
    Assert.IsNotNull(dedicatedThread);
    EnqueueWorkItems(dedicatedThread, mres);
    Assert.IsTrue(dedicatedThread.QueueCount > 0);
    Assert.IsTrue(dedicatedThread.IsBlocked);

    // Stop will send an event to the wait handle to resume so that it may exit
    dedicatedThread.StopImmediately();
    Expect.IsTrue("WaitHandle Stop Immediately",
      dedicatedThread.thread.Join(TimeSpan.FromMilliseconds(ThreadJoinTimeout)));
    mres.Reset();

    Expect.IsTrue("Stop Immediately", dedicatedThread.QueueCount > 0 && dedicatedThread.Terminated);
    dedicatedThread.Release();
  }

  private static void EnqueueWorkItems(DedicatedThread thread, ManualResetEventSlim resetEvent)
  {
    AsyncLongOperationAction workOp;
    for (int i = 0; i < 3; i++)
    {
      workOp = AsyncPool<AsyncLongOperationAction>.Get();
      workOp.OnInvoke += () => SleepThread(ItemWorkMS);
      thread.EnqueueSilently(workOp);
    }

    // Set wait handle in the last one so we can resume test execution
    workOp = AsyncPool<AsyncLongOperationAction>.Get();
    workOp.OnInvoke += () => SleepThread(ItemWorkMS, mres: resetEvent);
    thread.EnqueueSilently(workOp);
  }

  private static void SleepThread(int waitTime, ManualResetEventSlim mres = null)
  {
    // Simulate work so we can validate that consumer thread has unblocked
    Thread.Sleep(waitTime);
    mres?.Set();
  }
}