using System;
using System.Collections.Generic;
using System.Threading;
using SmashTools.Performance;

namespace SmashTools.Debugging
{
  internal class UnitTestDedicatedThread : UnitTest
  {
    private const int ThreadJoinTimeout = 5000;
    private const int WaitTime = 1000;
    private const int ItemWorkMS = WaitTime / 10;

    public override string Name => "DedicatedThread";

    public override TestType ExecuteOn => TestType.MainMenu;

    public override IEnumerable<UTResult> Execute()
    {
      UTResult result = new();

      DedicatedThread dedicatedThread = ThreadManager.CreateNew();
      // Should never start suspended
      Assert.IsFalse(dedicatedThread.IsSuspended);

      ManualResetEventSlim mres = new(false);

      // No signal should be received, it should've already entered a blocked state while waiting 
      // for an item to enqueue.
      result.Add("DedicatedThread (Polling Blocked)", dedicatedThread.IsBlocked);

      AsyncLongOperationAction pollingOp = AsyncPool<AsyncLongOperationAction>.Get();
      pollingOp.Set(() => SleepThread(ItemWorkMS, mres: mres));
      dedicatedThread.Enqueue(pollingOp);
      // Signal should be received this time, enqueueing item will set the event handler and resume
      // the thread's execution.
      result.Add("DedicatedThread (Polling Unblocked)", !dedicatedThread.IsBlocked);

      if (!mres.Wait(TimeSpan.FromMilliseconds(WaitTime)))
        result.Add("DedicatedThread (Wait Polling)", UTResult.Result.Failed);
      mres.Reset();

      Assert.IsTrue(dedicatedThread.QueueCount == 0);
      result.Add("DedicatedThread (Polling Reblocked)", dedicatedThread.IsBlocked);

      EnqueueWorkItems(dedicatedThread, mres);
      Assert.IsTrue(dedicatedThread.QueueCount > 0);
      Assert.IsTrue(dedicatedThread.IsBlocked);

      // Stop will send an event to the wait handle to resume so that it may exit
      dedicatedThread.Stop();
      // Allow WaitTime limit for each item in queue, but it should take nowhere near this long.
      if (!dedicatedThread.thread.Join(TimeSpan.FromMilliseconds(ThreadJoinTimeout)))
        result.Add("DedicatedThread (Wait Stop)", UTResult.Result.Failed);
      mres.Reset();

      result.Add("DedicatedThread (Stop Gracefully)",
        dedicatedThread.QueueCount == 0 && dedicatedThread.Terminated);
      dedicatedThread.Release();

      // Start a new thread so we can check immediate stop
      dedicatedThread = ThreadManager.CreateNew();
      EnqueueWorkItems(dedicatedThread, mres);
      Assert.IsTrue(dedicatedThread.QueueCount > 0);
      Assert.IsTrue(dedicatedThread.IsBlocked);

      // Stop will send an event to the wait handle to resume so that it may exit
      dedicatedThread.StopImmediately();
      if (!dedicatedThread.thread.Join(TimeSpan.FromMilliseconds(ThreadJoinTimeout)))
        result.Add("DedicatedThread (Wait Stop)", UTResult.Result.Failed);
      mres.Reset();

      result.Add("DedicatedThread (Stop Immediately)",
        dedicatedThread.QueueCount > 0 && dedicatedThread.Terminated);
      dedicatedThread.Release();

      yield return result;
    }

    private static void EnqueueWorkItems(DedicatedThread thread, ManualResetEventSlim resetEvent)
    {
      AsyncLongOperationAction workOp;
      for (int i = 0; i < 3; i++)
      {
        workOp = AsyncPool<AsyncLongOperationAction>.Get();
        workOp.Set(() => SleepThread(ItemWorkMS));
        thread.EnqueueSilently(workOp);
      }

      // Set wait handle in the last one so we can resume test execution
      workOp = AsyncPool<AsyncLongOperationAction>.Get();
      workOp.Set(() => SleepThread(ItemWorkMS, mres: resetEvent));
      thread.EnqueueSilently(workOp);
    }

    private static void SleepThread(int waitTime, ManualResetEventSlim mres = null)
    {
      // Simulate work so we can validate that consumer thread has unblocked
      Thread.Sleep(waitTime);
      mres?.Set();
    }
  }
}