using System.Collections.Generic;
using SmashTools.Performance;
using AsyncPool =
  SmashTools.Performance.AsyncPool<SmashTools.Debugging.UnitTestAsyncPool.TestObject>;

namespace SmashTools.Debugging;

internal class UnitTestAsyncPool : UnitTest
{
  public override string Name => "AsyncPool";

  public override TestType ExecuteOn => TestType.MainMenu;

  public override IEnumerable<UTResult> Execute()
  {
    const int PreWarmCount = 5;

    UTResult result = new();

    Assert.IsTrue(AsyncPool.Count == 0);

    // PreWarm
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      AsyncPool.PreWarm(PreWarmCount);
      result.Add("AsyncPool (PreWarm Init)", AsyncPool.Count == PreWarmCount);
      result.Add("AsyncPool (New Objects)", ocw.Count == PreWarmCount);
    }

    TestObject testObject;
    // Get
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      testObject = AsyncPool.Get();
      result.Add("ObjectPool (Get Item--)", AsyncPool.Count == (PreWarmCount - 1));
      result.Add("ObjectPool (Get New Objects)", ocw.Count == 0);
    }

    // Return
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      AsyncPool.Return(testObject);
      result.Add("AsyncPool (Return Item++)", AsyncPool.Count == PreWarmCount);
      result.Add("AsyncPool (Return New Objects)", ocw.Count == 0);
    }

    // Dump
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      AsyncPool.Clear();
      result.Add("ObjectPool (Dump Items)", AsyncPool.Count == 0);
      result.Add("ObjectPool (Dump New Objects)", ocw.Count == 0);
    }

    // Get (Create New)
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      _ = AsyncPool.Get();
      result.Add("ObjectPool (Get New Objects)", ocw.Count == 1);
    }

    AsyncPool.Clear();
    yield return result;
  }

  internal class TestObject
  {
    public TestObject()
    {
      ObjectCounter.Increment<TestObject>();
    }
  }
}