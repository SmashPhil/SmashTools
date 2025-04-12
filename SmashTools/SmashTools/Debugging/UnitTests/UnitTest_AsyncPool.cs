using DevTools;
using DevTools.UnitTesting;
using SmashTools.Performance;
using AsyncPool =
  SmashTools.Performance.AsyncPool<SmashTools.UnitTesting.UnitTest_AsyncPool.TestObject>;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
internal class UnitTest_AsyncPool
{
  [Prepare, CleanUp]
  private void ClearPool()
  {
    AsyncPool.Clear();
  }

  [Test]
  private void AsyncObjectPool()
  {
    const int PreWarmCount = 5;

    Assert.IsTrue(AsyncPool.Count == 0);

    // PreWarm
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      AsyncPool.PreWarm(PreWarmCount);
      Expect.IsTrue("PreWarm Init", AsyncPool.Count == PreWarmCount);
      Expect.IsTrue("New Objects", ocw.Count == PreWarmCount);
    }

    TestObject testObject;
    // Get
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      testObject = AsyncPool.Get();
      Expect.IsTrue("Get Item--", AsyncPool.Count == (PreWarmCount - 1));
      Expect.IsTrue("Get New Objects", ocw.Count == 0);
    }

    // Return
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      AsyncPool.Return(testObject);
      Expect.IsTrue("Return Item++", AsyncPool.Count == PreWarmCount);
      Expect.IsTrue("Return New Objects", ocw.Count == 0);
    }

    // Dump
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      AsyncPool.Clear();
      Expect.IsTrue("Dump Items", AsyncPool.Count == 0);
      Expect.IsTrue("Dump New Objects", ocw.Count == 0);
    }

    // Get (Create New)
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      _ = AsyncPool.Get();
      Expect.IsTrue("Get New Objects", ocw.Count == 1);
    }
  }

  internal class TestObject
  {
    public TestObject()
    {
      ObjectCounter.Increment<TestObject>();
    }
  }
}