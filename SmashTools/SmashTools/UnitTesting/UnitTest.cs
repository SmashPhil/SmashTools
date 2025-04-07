using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools.UnitTesting
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public abstract class UnitTest
  {
    public abstract TestType ExecuteOn { get; }

    public abstract string Name { get; }

    public virtual ExecutionPriority Priority => ExecutionPriority.Normal;

    public abstract IEnumerable<UTResult> Execute();

    public virtual void SetUp()
    {
    }

    public virtual void CleanUp()
    {
    }
  }
}