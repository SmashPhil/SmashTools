using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib;

/// <summary>
/// Destroys the GameObject when this struct goes out of scope.
/// </summary>
[PublicAPI]
public readonly struct DestroyOnDispose(GameObject obj) : IDisposable
{
  void IDisposable.Dispose()
  {
    Object.Destroy(obj);
  }
}
