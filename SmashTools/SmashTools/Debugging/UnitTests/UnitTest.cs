using System;
using System.Collections.Generic;

namespace SmashTools.Debugging
{
  public abstract class UnitTest
  {
    public abstract TestType ExecuteOn { get; }

    public abstract string Name { get; }

    public virtual ExecutionPriority Priority => ExecutionPriority.Normal;

    public abstract IEnumerable<UTResult> Execute();

    public enum TestType
    {
      Disabled = 0,
      MainMenu,
      GameLoaded,
    }

    public enum ExecutionPriority : int
    {
      Last = -2,
      Low = -1,
      Normal = 0,
      High = 1,
      First = 2
    }

    public static string TestTypeLabel(TestType testType)
    {
      return testType switch
      {
        TestType.Disabled   => "Disabled",
        TestType.MainMenu   => "Main Menu",
        TestType.GameLoaded => "Loaded Game",
        _                   => throw new System.NotImplementedException(),
      };
    }
  }

  public struct UTResult
  {
    private readonly Action onFail;

    public UTResult(Action onFail)
    {
      this.onFail = onFail;
    }

    public UTResult(string name, Result result)
    {
      string adjustedName = !name.NullOrEmpty() ? $"{name} = " : string.Empty;
      Add(adjustedName, result);
    }

    public List<(string name, Result result)> Results { get; private set; }

    public void Add(string name, Result result)
    {
      Results ??= [];
      Results.Add((name, result));
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
}