using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
  public class ObjectPool<T> where T : class, IPoolable, new()
  {
    private readonly T[] pool;
    private int head = 0;
    
    /// <summary>
    /// Creates fixed size object pool
    /// </summary>
    /// <param name="generator">Null generator will attempt to invoke default constructor.</param>
    public ObjectPool(int size)
    {
      pool = new T[size];
    }

    public bool Return(T item)
    {
      if (pool.OutOfBounds(head)) return false;

      item.Reset();
      pool[head] = item;
      if (head < pool.Length - 1) head++;
      item.InPool = true;
      return true;
    }

    public T Get()
    {
      T item = pool[head];
      pool[head] = null;
      item ??= new T();
      item.InPool = false;
      if (head > 0) head--;
      return item;
    }
  }
}
