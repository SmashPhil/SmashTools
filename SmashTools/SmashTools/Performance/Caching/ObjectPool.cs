using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace SmashTools.Performance;

public class ObjectPool<T> where T : class
{
  // Raw stack implementation for fast retrieval and insertion
  // with no auto-resizing.
  private readonly T[] pool;
  private int head;

  private readonly Func<T> factory;

  private readonly object poolLock = new();

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  public ObjectPool(int size)
  {
    var ctor = typeof(T).GetConstructor(Type.EmptyTypes);
    if (ctor == null)
      throw new ArgumentException("ObjectPool with no factory method must contain parameterless constructor for type T.");

    pool = new T[size];
    factory = MethodBuilder<T>.GetConstructor(ctor);
  }

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  /// <param name="factory">
  /// Factory method for creating new instances of <typeparam name="T"/>
  /// when new object needs to be instantiated for pooling.
  /// </param>
  public ObjectPool(int size, Func<T> factory)
  {
    pool = new T[size];
    this.factory = factory;
  }

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  /// <param name="preWarm">
  /// Add new objects to pool upon initialization.
  /// </param>
  public ObjectPool(int size, int preWarm) : this(size)
  {
    PreWarm(preWarm);
  }

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  /// <param name="preWarm">
  /// Add new objects to pool upon initialization.
  /// </param>
  /// <param name="factory">
  /// Factory method for creating new instances of <typeparam name="T"/>
  /// when new object needs to be instantiated for pooling.
  /// </param>
  public ObjectPool(int size, int preWarm, Func<T> factory) : this(size, factory)
  {
    PreWarm(preWarm);
  }

  /// <summary>
  /// Head of internal stack
  /// </summary>
  // NOTE - this is just for unit testing and debugging, the warnings here are
  // unwarranted as long as the context in which this is used does not change.
  // ReSharper disable once ConvertToAutoProperty
  // ReSharper disable once InconsistentlySynchronizedField
  public int Count => head;

  /// <summary>
  /// Add <paramref name="item"/> to pool.
  /// </summary>
  /// <remarks>
  /// If pool has hit capacity, item reference will be lost and at the mercy of GC.
  /// </remarks>
  public void Return(T item)
  {
    lock (poolLock)
    {
      if (pool.OutOfBounds(head))
        return;

      IPoolable poolable = item as IPoolable;
      poolable?.Reset();
      pool[head] = item;
      if (head < pool.Length - 1)
      {
        head++;
      }
      poolable?.InPool = true;
    }
  }

  /// <summary>
  /// Get object from pool. If pool is empty, a new object will be created and returned instead.
  /// </summary>
  public T Get()
  {
    lock (poolLock)
    {
      if (head == 0)
        return factory();

      head--;
      T item = pool[head];
      pool[head] = null;
      IPoolable poolable = item as IPoolable;
      poolable?.InPool = false;
      return item;
    }
  }

  /// <summary>
  /// Get object from pool scoped to <see cref="Scope"/> object's lifetime.
  /// </summary>
  /// <remarks>Returns to pool when the <see cref="Scope"/> object is disposed.</remarks>
  /// <param name="obj">Object acquired from pool.</param>
  /// <returns>Disposable <see cref="Scope"/> object.</returns>
  public Scope GetTemporary(out T obj)
  {
    obj = Get();
    return new Scope(this, obj);
  }

  /// <summary>
  /// Ensure objects already populate the object pool. If object pool count does not 
  /// exceed <paramref name="count"/>, new objects will be created to populate the pool.
  /// </summary>
  /// <remarks>
  /// If pooled objects already exist from 0 to <paramref name="count"/>, 
  /// no new objects will be added.
  /// </remarks>
  public void PreWarm(int count)
  {
    lock (poolLock)
    {
      int countToAdd = count - head;
      if (countToAdd > 0)
      {
        for (int i = 0; i < countToAdd; i++)
        {
          Return(factory());
        }
      }
    }
  }

  /// <summary>
  /// Remove all objects from pool and reset head to 0.
  /// </summary>
  public void Dump()
  {
    lock (poolLock)
    {
      while (head > 0)
      {
        _ = Get();
      }
      Assert.AreEqual(Count, 0);
      Assert.IsNull(pool[0]);
    }
  }

  /// <summary>
  /// Scoped acquisition of ObjectPool object.
  /// </summary>
  [PublicAPI]
  public readonly struct Scope : IDisposable
  {
    private readonly ObjectPool<T> pool;
    private readonly T item;

    public Scope(ObjectPool<T> pool, in T item)
    {
      this.pool = pool;
      this.item = item;
    }

    void IDisposable.Dispose()
    {
      pool.Return(item);
    }
  }
}