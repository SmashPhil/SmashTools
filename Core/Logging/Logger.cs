using JetBrains.Annotations;
using UnityEngine;

namespace CoreLib;

[PublicAPI]
public static class Logger
{
  private static ILogger impl;

  public static void Link<T>() where T : ILogger, new()
  {
    impl = new T();
  }

  public static void Message(string message)
  {
    if (impl == null)
    {
      Debug.Log(message);
      return;
    }

    impl.Message(message);
  }

  public static void Warning(string message)
  {
    if (impl == null)
    {
      Debug.LogWarning(message);
      return;
    }

    impl.Warning(message);
  }

  public static void Error(string message)
  {
    if (impl == null)
    {
      Debug.LogError(message);
      return;
    }

    impl.Error(message);
  }

  public interface ILogger
  {
    void Message(string message);
    void Warning(string message);
    void Error(string message);
  }
}