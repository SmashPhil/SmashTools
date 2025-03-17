using System.Diagnostics;
using UnityEngine;
using Verse;

namespace SmashTools
{
  // Reimplementation of System.Diagnostics.Trace with IMGUI popup and RimWorld logger
  public static class Trace
  {
    [Conditional("TRACE")]
    public static void IsTrue(bool condition, string message = null)
    {
      if (condition) return;
      Fail(message);
    }

    [Conditional("TRACE")]
    public static void IsFalse(bool condition, string message = null)
    {
      if (!condition) return;
      Fail(message);
    }

    [Conditional("TRACE")]
    public static void IsNull<T>(T obj, string message = null) where T : class
    {
      if (obj == null) return;
      Fail(message);
    }

    [Conditional("TRACE")]
    public static void IsNotNull<T>(T obj, string message = null) where T : class
    {
      if (obj != null) return;
      Fail(message);
    }

    [Conditional("TRACE")]
    public static void Fail(string message = null)
    {
#if DEBUG
      Assert.Fail(message);
#else
      Log.Error(
        $"{message ?? "Assertion Failed"}\nStackTrace:\n{StackTraceUtility.ExtractStackTrace()}");
#endif
    }
  }
}