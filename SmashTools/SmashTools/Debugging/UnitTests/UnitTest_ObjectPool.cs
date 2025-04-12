using System.Collections.Generic;
using System.Linq;
using DevTools;
using DevTools.UnitTesting;
using SmashTools.Performance;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
internal class UnitTest_ObjectPool
{
  [Test]
  private void SynchronousObjectPool()
  {
    const int PreWarmCount = 5;

    ObjectPool<TestObject> pool = new(10);
    Assert.IsTrue(pool.Count == 0);

    // PreWarm
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      pool.PreWarm(PreWarmCount);
      Expect.IsTrue("PreWarm Init", pool.Count == PreWarmCount);
      Expect.IsTrue("PreWarm InPool)", pool.All(obj => obj.InPool));
      Expect.IsTrue("PreWarm Reset", pool.All(obj => obj.IsReset));
      Expect.IsTrue("New Objects", ocw.Count == PreWarmCount);
    }

    // Create new object before we start watching object count, in practice
    // this object would've already been in use before being returned to pool.
    TestObject testObject = new();

    // Return
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      pool.Return(testObject);
      Expect.IsTrue("Return InPool", testObject.InPool);
      Expect.IsTrue("Return Reset", testObject.IsReset);
      Expect.IsTrue("Return Head++", pool.Count == (PreWarmCount + 1));
      Expect.IsTrue("Return New Objects", ocw.Count == 0);
    }

    // Get
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      TestObject fetchedObject = pool.Get();
      Assert.IsFalse(testObject.IsReset);
      Expect.IsTrue("Get Head", testObject == fetchedObject);
      Expect.IsFalse("Get InPool", testObject.InPool);
      Expect.IsTrue("Get Head--", pool.Count == PreWarmCount);
      Expect.IsTrue("Get New Objects", ocw.Count == 0);
    }

    // Dump
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      pool.Dump();
      Expect.IsTrue("Dump Head", pool.Count == 0);
      Expect.IsTrue("Dump New Objects", ocw.Count == 0);
    }

    // Get (Create New)
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      _ = pool.Get();
      Expect.IsTrue("Get New Objects", ocw.Count == 1);
    }
  }

  private class TestObject : IPoolable
  {
    private bool inPool;

    public TestObject()
    {
      ObjectCounter.Increment<TestObject>();
    }

    public bool IsReset { get; private set; }

    // Need backing field so we can simulate changing values when
    // object is fetched from pool by setting IsReset to false.
    public bool InPool
    {
      get { return inPool; }
      set
      {
        if (inPool == value) return;

        inPool = value;
        if (!inPool) IsReset = false;
      }
    }

    void IPoolable.Reset()
    {
      IsReset = true;
    }
  }
}