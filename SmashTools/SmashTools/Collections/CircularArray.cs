using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// A fixed-size circular buffer that overwrites the oldest elements when new elements are pushed beyond its capacity.
/// </summary>
/// <typeparam name="T">Type of elements stored in the circular buffer.</typeparam>
[PublicAPI]
public class CircularArray<T> : IEnumerable<T>
{
  private readonly T[] array;
  private int head;
  private int tail;

  /// <summary>
  /// Initializes a new instance of the <see cref="CircularArray{T}"/> class with the specified capacity.
  /// </summary>
  /// <param name="size">The fixed capacity of the circular array.</param>
  public CircularArray(int size)
  {
    array = new T[size];
  }

  /// <summary>
  /// Gets the total capacity of the circular buffer.
  /// </summary>
  public int Length => array.Length;

  /// <summary>
  /// Gets the underlying backing array for inspection or advanced operations.
  /// </summary>
  public T[] InnerArray => array;

  /// <summary>
  /// Gets the element at the specified logical index, accounting for wrap-around.
  /// </summary>
  /// <param name="index">The zero-based logical index within the buffer.</param>
  /// <returns>The element at the given logical position.</returns>
  public T this[int index]
  {
    get
    {
      int realIndex = GenMath.PositiveMod(tail + index, Length);
      return array[realIndex];
    }
  }

  /// <summary>
  /// Adds an item to the head of the buffer, overwriting the oldest element if the buffer is full.
  /// </summary>
  /// <param name="item">The item to push into the buffer.</param>
  /// <returns>The element that was dropped (overwritten), or default(T) if the slot was empty.</returns>
  public T Push(T item)
  {
    T dropped = array[head];
    array[head] = item;
    head = GenMath.PositiveMod(++head, Length);
    if (head == tail)
      tail = GenMath.PositiveMod(++tail, Length);
    return dropped;
  }

  /// <summary>
  /// Removes (resets to default) the element at the specified logical index without shifting other elements.
  /// </summary>
  /// <param name="index">The zero-based logical index of the element to remove.</param>
  public void RemoveAt(int index)
  {
    int realIndex = GenMath.PositiveMod(tail + index, Length);
    array[realIndex] = default!;
  }

  /// <summary>
  /// Returns an enumerator that iterates through the elements in the buffer from oldest to newest.
  /// </summary>
  /// <returns>An enumerator for the buffer contents.</returns>
  public IEnumerator<T> GetEnumerator()
  {
    for (int i = head; i != tail; i = GenMath.PositiveMod(++i, Length))
    {
      yield return array[i];
    }
  }

  /// <summary>
  /// Returns a non-generic enumerator that iterates through the elements in the buffer.
  /// </summary>
  /// <returns>An <see cref="IEnumerator"/> for the buffer contents.</returns>
  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}