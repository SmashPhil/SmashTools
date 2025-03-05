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
  }

  public struct UTResult
  {
    public UTResult(string name, bool passed)
    {
      string adjustedName = !name.NullOrEmpty() ? $"{name} = " : string.Empty;
      Results = [(adjustedName, passed)];
    }

    public List<(string name, bool result)> Results { get; private set; }

    public void Add(string name, bool passed)
    {
      Results ??= [];
      Results.Add((name, passed));
    }

    public static UTResult For(string name, bool passed)
    {
      return new UTResult(name, passed);
    }
  }
}
