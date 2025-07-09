using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Records the current value of a collection and rolls it back when this struct goes out of scope.
/// </summary>
/// <remarks>Recording a non-empty collection will create a shallow copy of the collection object.</remarks>
/// <typeparam name="T">Type of the collection being rolled back.</typeparam>
[PublicAPI]
public readonly struct ClearOnDispose<T> : IDisposable
{
  private readonly ICollection<T> value;

  public ClearOnDispose(ICollection<T> obj)
  {
    value = obj;
  }

  void IDisposable.Dispose()
  {
    value?.Clear();
  }
}