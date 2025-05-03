using System.Diagnostics;
using UnityEngine;
using Verse;

namespace SmashTools.Debugging;

public static class AssertHandler
{
  [Conditional("UNITY_ASSERTIONS")]
  public static void Enable()
  {
    if (!UnityData.IsInMainThread)
    {
      Log.Error("AssertHandler.Enable can only be called on the main thread.");
      return;
    }
    Application.logMessageReceivedThreaded += OnAssertThrow;
  }

  [Conditional("UNITY_ASSERTIONS")]
  public static void Disable()
  {
    if (!UnityData.IsInMainThread)
    {
      Log.Error("AssertHandler.Disable can only be called on the main thread.");
      return;
    }
    Application.logMessageReceivedThreaded -= OnAssertThrow;
  }

  private static void OnAssertThrow(string condition, string stackTrace, LogType type)
  {
    if (type == LogType.Assert)
    {
      Log.Error($"{condition}\n{stackTrace}");
    }
  }
}