using System.Collections.Generic;

namespace SmashTools.Targeting;

public readonly struct TargetData<T>
{
  public readonly List<T> targets;

  public TargetData()
  {
    targets = [];
  }
}