using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;
using Logger = CoreLib.Logger;

// TODO 1.7 - Change to CoreLib
namespace SmashTools;

/// <summary>
/// Log wrapper class for extracting stack trace info before sending to log
/// </summary>
[PublicAPI]
[TypeForwardedFrom(assemblyFullName: "SmashTools, Version=1.6.0.0, Culture=neutral, PublicKeyToken=null")]
public static class Trace
{
  [Conditional("TRACE")]
  public static void IsTrue(bool condition, string message = null)
  {
    if (condition)
      return;
    Fail(message);
  }

  [Conditional("TRACE")]
  public static void IsFalse(bool condition, string message = null)
  {
    if (!condition)
      return;
    Fail(message);
  }

  [Conditional("TRACE")]
  public static void IsNull<T>(T obj, string message = null) where T : class
  {
    if (obj == null)
      return;
    Fail(message);
  }

  [Conditional("TRACE")]
  public static void IsNotNull<T>(T obj, string message = null) where T : class
  {
    if (obj != null)
      return;
    Fail(message);
  }

  [Conditional("TRACE")]
  public static void Fail(string message = null)
  {
    Logger.Error(
      $"{message ?? "Assertion Failed"}\nStackTrace:\n{StackTraceUtility.ExtractStackTrace()}");
  }
}