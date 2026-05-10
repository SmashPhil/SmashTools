using System;
using JetBrains.Annotations;

namespace CoreLib.PathFinding;

[PublicAPI]
public interface IPathPromise : IDisposable
{
  bool IsCompleted { get; }

  Path GetPath();

  void Cancel();
}