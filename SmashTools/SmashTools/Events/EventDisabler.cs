using System;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Temporarily disables event triggers on the specified <see cref="IEventManager{T}"/>.
/// When disposed, event dispatching is re-enabled.
/// </summary>
/// <typeparam name="T">The type of event keys managed by the event manager.</typeparam>
[PublicAPI]
public readonly struct EventDisabler<T> : IDisposable
{
  private readonly IEventManager<T> manager;
  private readonly T[] disableSpecific;

  /// <summary>
  /// Initializes a new <see cref="EventDisabler{T}"/>, disabling all events or only the specified event keys.
  /// </summary>
  /// <param name="manager">The event manager whose events will be disabled.</param>
  /// <param name="disableSpecific">
  /// An optional array of event keys to suppress. If omitted or empty, all events are disabled.
  /// </param>
  public EventDisabler(IEventManager<T> manager, params T[] disableSpecific)
  {
    this.manager = manager;
    this.disableSpecific = disableSpecific;
    SetState(false);
  }

  /// <summary>
  /// Restores the previous event-enabled state when the disabler goes out of scope.
  /// </summary>
  void IDisposable.Dispose()
  {
    SetState(true);
  }

  /// <summary>
  /// Toggles the enabled state of the event registry or specific event triggers.
  /// </summary>
  /// <param name="enabled">
  /// <see langword="true"/> to enable dispatching; <see langword="false"/> to suppress it.
  /// </param>
  private void SetState(bool enabled)
  {
    if (!disableSpecific.NullOrEmpty())
    {
      foreach (T key in disableSpecific)
      {
        IEventControl enabler = manager.EventRegistry[key];
        enabler.Enabled = enabled;
      }
    }
    else
    {
      IEventControl enabler = manager.EventRegistry;
      enabler.Enabled = enabled;
    }
  }
}