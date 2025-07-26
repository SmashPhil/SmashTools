using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Targeting;

[PublicAPI]
public struct TargeterResult
{
  public required TargeterAction action;
  public List<ITargetOption> options;

  public static TargeterResult None => new() { action = TargeterAction.None };

  public static TargeterResult Reject => new() { action = TargeterAction.Reject };

  public static TargeterResult Cancel => new() { action = TargeterAction.Cancel };

  public static TargeterResult Submit => new() { action = TargeterAction.Submit };

  public static TargeterResult Accept<T>(List<T> options) where T : ITargetOption
  {
    return new TargeterResult { action = TargeterAction.Accept, options = [.. options] };
  }
}