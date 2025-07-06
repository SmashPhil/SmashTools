using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools;

[PublicAPI]
public class EventTrigger
{
  private readonly List<Trigger> persistents = [];
  private readonly List<Trigger> singles = [];

  public bool Enabled { get; set; } = true;

  public bool Contains(string key)
  {
    foreach (Trigger trigger in persistents)
    {
      if (key != null && trigger.key != null && key == trigger.key)
        return true;
    }
    return false;
  }

  /// <summary>
  /// Persistent trigger is currently registered in event trigger
  /// </summary>
  public bool Contains(Action action)
  {
    foreach (Trigger trigger in persistents)
    {
      if (trigger.action == action)
        return true;
    }
    return false;
  }

  public void Add(string key, Action action)
  {
    persistents.Add(new Trigger(key, action));
  }

  public void AddSingle(string key, Action action)
  {
    singles.Add(new Trigger(key, action));
  }

  public int Remove(string key)
  {
    int count = 0;
    for (int i = persistents.Count - 1; i >= 0; i--)
    {
      Trigger trigger = persistents[i];
      if (trigger.key == key)
      {
        persistents.RemoveAt(i);
        count++;
      }
    }

    return count;
  }

  public int Remove(Action action)
  {
    int count = 0;
    for (int i = persistents.Count - 1; i >= 0; i--)
    {
      Trigger trigger = persistents[i];
      if (trigger.action == action)
      {
        persistents.RemoveAt(i);
        count++;
      }
    }
    return count;
  }

  public int RemoveSingle(string key)
  {
    int count = 0;
    for (int i = singles.Count - 1; i >= 0; i--)
    {
      Trigger trigger = singles[i];
      if (trigger.key == key)
      {
        singles.RemoveAt(i);
        count++;
      }
    }

    return count;
  }

  public int RemoveSingle(Action action)
  {
    int count = 0;
    for (int i = singles.Count - 1; i >= 0; i--)
    {
      Trigger trigger = singles[i];
      if (trigger.action == action)
      {
        singles.RemoveAt(i);
        count++;
      }
    }

    return count;
  }

  public void ClearAll()
  {
    singles.Clear();
    persistents.Clear();
  }

  public void ExecuteEvents()
  {
    if (!Enabled) return;

    foreach (Trigger trigger in persistents)
    {
      trigger.action();
    }

    for (int i = singles.Count - 1; i >= 0; i--)
    {
      Trigger trigger = singles[i];
      trigger.action();
      singles.RemoveAt(i);
    }
  }

  private readonly struct Trigger(string key, Action action)
  {
    public readonly string key = key;
    public readonly Action action = action;
  }
}