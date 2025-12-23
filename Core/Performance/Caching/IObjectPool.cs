using System;
using JetBrains.Annotations;

namespace SmashTools.Performance;

[PublicAPI]
public interface IObjectPool<T>
{
  void Return(T item);
  T Get();
  void Clear();
}

[PublicAPI]
public interface IObjectPool<T, out TScope> : IObjectPool<T> where TScope : struct, IDisposable
{
  TScope GetTemporary(out T obj);
}
