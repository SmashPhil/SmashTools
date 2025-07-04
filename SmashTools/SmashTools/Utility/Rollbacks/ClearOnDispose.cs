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
  private readonly ValueState state;
  private readonly ICollection<T> value;
  private readonly ICollection<T> shallowCopy;

  public ClearOnDispose(ICollection<T> obj)
  {
    value = obj;
    if (obj.Count == 0)
    {
      state = ValueState.Empty;
    }
    else
    {
      shallowCopy = obj.ToList();
      state = ValueState.Populated;
    }
  }

  void IDisposable.Dispose()
  {
    switch (state)
    {
      case ValueState.Empty:
        value?.Clear();
      break;
      case ValueState.Populated:
        value?.Clear();
        if (shallowCopy != null)
        {
          foreach (T item in shallowCopy)
            value?.Add(item);
        }
      break;
      default:
        throw new NotImplementedException(state.ToString());
    }
  }

  private enum ValueState
  {
    Empty,
    Populated
  }
}