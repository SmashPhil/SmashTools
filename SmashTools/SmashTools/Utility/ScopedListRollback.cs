using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Records the current value of a List and rolls it back when this struct goes out of scope.
/// </summary>
/// <remarks>Recording a non-empty list will create a shallow copy of the list object.</remarks>
/// <typeparam name="T">Type of the list being rolled back.</typeparam>
[PublicAPI]
public readonly struct ScopedListRollback<T> : IDisposable
{
  private readonly ValueState state;
  private readonly List<T> value;
  private readonly List<T> shallowCopy;

  public ScopedListRollback(List<T> obj)
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
        value.Clear();
      break;
      case ValueState.Populated:
        value.Clear();
        value.AddRange(shallowCopy);
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