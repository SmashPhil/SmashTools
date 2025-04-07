using System;
using System.Collections.Generic;

namespace SmashTools.UnitTesting;

public class UTResult
{
  public readonly string name;
  private readonly Action onFail;

  public UTResult()
  {
  }

  public UTResult(string name, Action onFail = null)
  {
    this.name = name;
    this.onFail = onFail;
  }

  // Single test result
  private UTResult(string name, Result result)
  {
    string adjustedName = !name.NullOrEmpty() ? $"{name} = " : string.Empty;
    Add(adjustedName, result);
  }

  public List<(string name, Result result)> Tests { get; private set; }

  public void Add(string name, Result result)
  {
    Tests ??= [];
    Tests.Add((name, result));
    if (result == Result.Failed) onFail?.Invoke();
  }

  public void Add(string name, bool passed)
  {
    Add(name, passed ? Result.Passed : Result.Failed);
  }

  public static UTResult For(string name, Result result)
  {
    return new UTResult(name, result);
  }

  public static UTResult For(string name, bool passed)
  {
    return new UTResult(name, passed ? Result.Passed : Result.Failed);
  }

  public enum Result
  {
    Failed,
    Passed,
    Skipped,
  }
}