using System;
using System.Text;

namespace SmashTools;

public readonly struct ClearStringOnDispose : IDisposable
{
  private readonly StringBuilder stringBuilder;

  public ClearStringOnDispose(StringBuilder stringBuilder)
  {
    this.stringBuilder = stringBuilder;
  }

  void IDisposable.Dispose()
  {
    stringBuilder?.Clear();
  }
}