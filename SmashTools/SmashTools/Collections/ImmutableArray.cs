using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools;

public class ImmutableArray<T> : IEnumerable<T>
{
  private volatile T[] items;

  public ImmutableArray(int size)
  {
    this.items = new T[size];
  }

  public ImmutableArray(IEnumerable<T> items)
  {
    this.items = [.. items];
  }

  // ReSharper disable LoopCanBeConvertedToQuery
  public IEnumerator<T> GetEnumerator()
  {
    foreach (T item in items)
    {
      yield return item;
    }
  }
  // ReSharper restore LoopCanBeConvertedToQuery

  IEnumerator IEnumerable.GetEnumerator()
  {
    return this.GetEnumerator();
  }
}