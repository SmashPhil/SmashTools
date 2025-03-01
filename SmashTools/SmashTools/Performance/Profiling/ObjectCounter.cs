using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;

namespace SmashTools.Performance
{
  // Simple counter for tracking object instantiation since RimWorld doesn't work
  // well with memory profilers. This is only useful for tracking what might be
  // putting pressure on GC. This is not a supplement for proper memory profilers,
  // and obviously it can't find memory leaks.
  public static class ObjectCounter
  {
    private static readonly ConcurrentDictionary<Type, int> counter = [];

    private static readonly ConcurrentDictionary<Type, int> countWatched = [];

    public static bool Clear<T>() => counter.TryRemove(typeof(T), out _);

    public static void ClearAll() => counter.Clear();

    [Conditional("DEBUG")]
    public static void Increment<T>()
    {
      if (!counter.ContainsKey(typeof(T))) counter[typeof(T)] = 0;
      counter[typeof(T)]++;
    }

    public static void LogAll()
    {
      foreach (Type type in counter.Keys)
      {
        Log(type);
      }
    }

    public static void Log(Type type)
    {
      if (!counter.TryGetValue(type, out int count))
      {
        count = 0;
      }
      Verse.Log.Message($"{type.Name} = {count}");
    }

    public static void StartWatcher<T>()
    {
      if (!counter.TryGetValue(typeof(T), out int current))
      {
        current = 0;
      }
      countWatched.TryAdd(typeof(T), current);
    }

    public static int GetWatchedCount<T>()
    {
      if (!countWatched.TryGetValue(typeof(T), out int watched))
      {
        Trace.Fail($"Ending watcher which hasn't been started.");
        return 0;
      }
      if (!counter.TryGetValue(typeof(T), out int current))
      {
        current = 0;
      }
      return current - watched;
    }

    public static int EndWatcher<T>()
    {
      int difference = GetWatchedCount<T>();
      countWatched.TryRemove(typeof(T), out _);
      return difference;
    }

#if DEBUG
    [DebugAction(category = "Mods", name = "Log Object Counters", actionType = DebugActionType.Action)]
    private static void LogAllDebugAction()
    {
      LogAll();
    }
#endif
  }
}
