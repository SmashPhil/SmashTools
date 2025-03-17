using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace SmashTools.Performance
{
  public class AsyncLongOperationAction : AsyncAction
  {
    private Action action;
    private Func<bool> isValid;

    public override bool LongOperation => true;

    public override bool IsValid => isValid?.Invoke() ?? true;

    public void Set(Action action, Func<bool> isValid = null)
    {
      this.action = action;
      this.isValid = isValid;
    }

    public override void Invoke()
    {
      action.Invoke();
    }

    public override void ReturnToPool()
    {
      action = null;
      isValid = null;
      AsyncPool<AsyncLongOperationAction>.Return(this);
    }
  }
}