using System;
using JetBrains.Annotations;

namespace SmashTools.Performance;

// ReSharper disable PossibleNullReferenceException
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AsyncLongOperationAction : AsyncAction
{
  public event Action OnInvoke;
  public event Func<bool> OnValidate;

  public override bool LongOperation => true;

  public override bool IsValid => OnInvoke != null && (OnValidate == null || OnValidate());

  public override void Invoke()
  {
    OnInvoke();
  }

  public override void ReturnToPool()
  {
    OnInvoke = null;
    OnValidate = null;
    AsyncPool<AsyncLongOperationAction>.Return(this);
  }
}