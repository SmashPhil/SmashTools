using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools;

public readonly struct EventDisabler<T> : IDisposable
{
  private readonly IEventManager<T> manager;
  private readonly T[] disableSpecific;

  public EventDisabler(IEventManager<T> manager, params T[] disableSpecific)
  {
    this.manager = manager;
    this.disableSpecific = disableSpecific;
    SetState(false);
  }

  private void SetState(bool enabled)
  {
    if (!disableSpecific.NullOrEmpty())
    {
      foreach (T key in disableSpecific)
      {
        manager.EventRegistry[key].Enabled = enabled;
      }
    }
    else
    {
      manager.EventRegistry.Enabled = enabled;
    }
  }

  void IDisposable.Dispose()
  {
    SetState(true);
  }
}