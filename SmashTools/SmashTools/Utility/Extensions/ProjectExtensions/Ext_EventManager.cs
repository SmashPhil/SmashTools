using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public static class Ext_EventManager
  {
    public static void FillEvents<T>(this IEventManager<T> manager, IEnumerable<T> events)
    {
      manager.EventRegistry = new EventManager<T>();
      foreach (T value in events)
      {
        manager.RegisterEventType(value);
      }
    }

    public static void FillEvents_Enum<T>(this IEventManager<T> manager)
    {
      if (!typeof(T).IsEnum)
      {
        Log.Error(
          $"Tried to fill IEventManager with enum values and non-enum type. Type=\"{typeof(T)}\" Manager=\"{manager}\"");
        return;
      }

      manager.EventRegistry = new EventManager<T>();
      foreach (T value in Enum.GetValues(typeof(T)))
      {
        manager.RegisterEventType(value);
      }
    }

    public static void FillEvents_Def<T>(this IEventManager<T> manager) where T : Def
    {
      manager.EventRegistry = new EventManager<T>();
      foreach (T def in DefDatabase<T>.AllDefsListForReading)
      {
        manager.RegisterEventType(def);
      }
    }

    public static void RegisterEventType<T>(this IEventManager<T> manager, T @event)
    {
      manager.EventRegistry[@event] = new EventTrigger();
    }

    public static void AddEvent<T>(this IEventManager<T> manager, T @event, Action action,
      params Action[] actions)
    {
      manager.EventRegistry[@event].Add(null, action);
      if (!actions.NullOrEmpty())
      {
        foreach (Action additionalAction in actions)
        {
          manager.EventRegistry[@event].Add(null, additionalAction);
        }
      }
    }

    public static void AddEvent<T>(this IEventManager<T> manager, T @event, Action action,
      string key, params Action[] actions)
    {
      manager.EventRegistry[@event].Add(key, action);
      if (!actions.NullOrEmpty())
      {
        foreach (Action additionalAction in actions)
        {
          manager.EventRegistry[@event].Add(key, additionalAction);
        }
      }
    }

    public static void AddSingleEvent<T>(this IEventManager<T> manager, T @event, Action action,
      params Action[] actions)
    {
      manager.EventRegistry[@event].AddSingle(null, action);
      if (!actions.NullOrEmpty())
      {
        foreach (Action additionalAction in actions)
        {
          manager.EventRegistry[@event].AddSingle(null, additionalAction);
        }
      }
    }

    public static void AddSingleEvent<T>(this IEventManager<T> manager, T @event, Action action,
      string key, params Action[] actions)
    {
      manager.EventRegistry[@event].AddSingle(key, action);
      if (!actions.NullOrEmpty())
      {
        foreach (Action additionalAction in actions)
        {
          manager.EventRegistry[@event].AddSingle(key, additionalAction);
        }
      }
    }

    public static void RemoveEvent<T>(this IEventManager<T> manager, T @event, string key)
    {
      manager.EventRegistry[@event].Remove(key);
    }

    public static void RemoveEvent<T>(this IEventManager<T> manager, T @event, Action action)
    {
      manager.EventRegistry[@event].Remove(action);
    }

    public static void RemoveSingleEvent<T>(this IEventManager<T> manager, T @event, string key)
    {
      manager.EventRegistry[@event].RemoveSingle(key);
    }

    public static void RemoveSingleEvent<T>(this IEventManager<T> manager, T @event, Action action)
    {
      manager.EventRegistry[@event].RemoveSingle(action);
    }

    public static void ClearAll<T>(this IEventManager<T> manager, T @event)
    {
      manager.EventRegistry[@event].ClearAll();
    }

    public static bool Initialized<T>(this EventManager<T> manager)
    {
      return manager != null && !manager.lookup.NullOrEmpty();
    }
  }
}