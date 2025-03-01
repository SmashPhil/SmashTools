using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Performance
{
  public readonly struct ObjectCountWatcher<T> : IDisposable
  {
    public ObjectCountWatcher()
    {
      ObjectCounter.StartWatcher<T>();
    }

    public readonly int Count => ObjectCounter.GetWatchedCount<T>();

    void IDisposable.Dispose()
    {
      ObjectCounter.EndWatcher<T>();
    }
  }
}
