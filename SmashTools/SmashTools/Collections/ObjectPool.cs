using System.Collections;
using System.Collections.Generic;
using DevTools;

namespace SmashTools
{
  public class ObjectPool<T> : IEnumerable<T> where T : class, IPoolable, new()
  {
    // Raw stack implementation for fast retrieval and insertion
    // with no auto-resizing.
    private readonly T[] pool;
    private int head;

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
    public ObjectPool(int size, int preWarm = 0)
    {
      pool = new T[size];
      PreWarm(preWarm);
    }

    /// <summary>
    /// Head of internal stack
    /// </summary>
    public int Count => head;

    /// <summary>
    /// Add <paramref name="item"/> to pool.
    /// </summary>
    /// <remarks>
    /// If pool has hit capacity, item reference will be lost and at the mercy of GC.
    /// </remarks>
    public bool Return(T item)
    {
      if (pool.OutOfBounds(head)) return false;

      item.Reset();
      pool[head] = item;
      if (head < pool.Length - 1) head++;
      item.InPool = true;
      return true;
    }

    /// <summary>
    /// Get object from pool. If pool is empty, a new object will be created and returned instead.
    /// </summary>
    public T Get()
    {
      if (head == 0) return new T();
      head--;
      T item = pool[head];
      pool[head] = null;
      item.InPool = false;
      return item;
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
      int countToAdd = count - head;
      if (countToAdd > 0)
      {
        for (int i = 0; i < countToAdd; i++)
        {
          Return(new T());
        }
      }
    }

    /// <summary>
    /// Remove all objects from pool and reset head to 0.
    /// </summary>
    public void Dump()
    {
      while (head > 0) _ = Get();
      Assert.IsTrue(Count == 0 && pool[0] == null);
    }

    public IEnumerator<T> GetEnumerator()
    {
      for (int i = 0; i < head; i++)
      {
        yield return pool[i];
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }
  }
}