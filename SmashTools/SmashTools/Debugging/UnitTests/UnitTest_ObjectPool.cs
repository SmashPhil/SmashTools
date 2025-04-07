using System.Collections.Generic;
using System.Linq;
using DevTools;
using SmashTools.Performance;

namespace SmashTools.UnitTesting
{
  internal class UnitTest_ObjectPool : UnitTest
  {
    public override string Name => "ObjectPool";

    public override TestType ExecuteOn => TestType.MainMenu;

    public override IEnumerable<UTResult> Execute()
    {
      const int PreWarmCount = 5;

      UTResult result = new();

      ObjectPool<TestObject> pool = new(10);
      Assert.IsTrue(pool.Count == 0);

      // PreWarm
      {
        using ObjectCountWatcher<TestObject> ocw = new();

        pool.PreWarm(PreWarmCount);
        result.Add("ObjectPool (PreWarm Init)", pool.Count == PreWarmCount);
        result.Add("ObjectPool (PreWarm InPool)", pool.All(obj => obj.InPool));
        result.Add("ObjectPool (PreWarm Reset)", pool.All(obj => obj.IsReset));
        result.Add("ObjectPool (New Objects)", ocw.Count == PreWarmCount);
      }

      // Create new object before we start watching object count, in practice
      // this object would've already been in use before being returned to pool.
      TestObject testObject = new();

      // Return
      {
        using ObjectCountWatcher<TestObject> ocw = new();

        pool.Return(testObject);
        result.Add("ObjectPool (Return InPool)", testObject.InPool);
        result.Add("ObjectPool (Return Reset)", testObject.IsReset);
        result.Add("ObjectPool (Return Head++)", pool.Count == (PreWarmCount + 1));
        result.Add("ObjectPool (Return New Objects)", ocw.Count == 0);
      }

      // Get
      {
        using ObjectCountWatcher<TestObject> ocw = new();

        TestObject fetchedObject = pool.Get();
        Assert.IsFalse(testObject.IsReset);
        result.Add("ObjectPool (Get Head)", testObject == fetchedObject);
        result.Add("ObjectPool (Get InPool)", !testObject.InPool);
        result.Add("ObjectPool (Get Head--)", pool.Count == PreWarmCount);
        result.Add("ObjectPool (Get New Objects)", ocw.Count == 0);
      }

      // Dump
      {
        using ObjectCountWatcher<TestObject> ocw = new();
        pool.Dump();
        result.Add("ObjectPool (Dump Head)", pool.Count == 0);
        result.Add("ObjectPool (Dump New Objects)", ocw.Count == 0);
      }

      // Get (Create New)
      {
        using ObjectCountWatcher<TestObject> ocw = new();
        _ = pool.Get();
        result.Add("ObjectPool (Get New Objects)", ocw.Count == 1);
      }
      yield return result;
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
}