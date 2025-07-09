using System;
using System.Collections.Generic;
using Verse;

namespace SmashTools.Targeting;

public static class TargeterDispatcher
{
  private static readonly Stack<ITargeter> Targeters = [];

  private static ITargeter Current { get; set; }

  internal static void TargeterUpdate()
  {
    if (Current == null)
      return;

    try
    {
      Current.Update();
    }
    catch
    {
      // Remove problematic targeter or we'll end up spamming incessantly
      Targeters.Pop();
      throw;
    }
  }

  internal static void TargeterOnGUI()
  {
    if (Current == null)
      return;
    try
    {
      Current.OnGUI();
    }
    catch
    {
      // Remove problematic targeter or we'll end up spamming incessantly
      Targeters.Pop();
      UpdateCurrent();
      throw;
    }
  }

  public static void Start(this ITargeter targeter)
  {
    Targeters.Push(targeter);
    UpdateCurrent();
  }

  public static void Stop(this ITargeter targeter)
  {
    if (Targeters.Count == 0)
      throw new InvalidOperationException("Trying to stop targeter but the targeter stack is empty.");

    if (Targeters.Peek() == targeter)
    {
      Targeters.Pop();
    }
    else
    {
      Log.Error("Removing targeter out of sequence.");
      Remove(targeter);
    }
    UpdateCurrent();
  }

  private static void UpdateCurrent()
  {
    Current = Targeters.Peek();
  }

  private static void Remove(ITargeter targeter)
  {
    Stack<ITargeter> buffer = [];
    while (Targeters.Count > 0)
    {
      ITargeter top = Targeters.Pop();
      if (top == targeter)
        break;
      buffer.Push(top);
    }
    while (buffer.Count > 0)
      Targeters.Push(buffer.Pop());
  }
}