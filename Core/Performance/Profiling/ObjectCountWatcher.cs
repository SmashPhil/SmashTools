using System;
using CoreLib.Performance;

namespace CoreLib.Performance;

public readonly struct ObjectCountWatcher<T> : IDisposable
{
  public ObjectCountWatcher()
  {
    ObjectCounter.StartWatcher<T>();
  }

  public int Count => ObjectCounter.GetWatchedCount<T>();

  void IDisposable.Dispose()
  {
    ObjectCounter.EndWatcher<T>();
  }
}