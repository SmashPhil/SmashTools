using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools
{
	/// <summary>
	/// Concurrent implementation of a limited size stack. If capacity is reached, stack will begin 'dropping' elements from the bottom.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DropOutStack<T> : IEnumerable<T>
	{
		private readonly T[] items;
		private int top;
		private int count;

		public DropOutStack(int capacity)
		{
			this.items = new T[capacity];
		}

		public int Count => count;

		public void Push(T item)
		{
			lock(this)
			{
				items[top] = item;
				top = GenMath.PositiveMod(++top, items.Length);
				count = Mathf.Clamp(++count, 0, items.Length);
			}
		}

		public T Pop()
		{
			if (Count == 0)
			{
				throw new InvalidOperationException("Empty stack");
			}
			lock (this)
			{
				top = GenMath.PositiveMod(items.Length + --top, items.Length);
				T item = items[top];
				items[top] = default;
				count = Mathf.Clamp(--count, 0, items.Length);
				return item;
			}
		}

		public bool TryPop(out T item)
		{
			if (Count == 0)
			{
				item = default;
				return false;
			}
			item = Pop();
			return true;
		}

		public T Peek()
		{
			return items[top];
		}

		public IEnumerator<T> GetEnumerator()
		{
			lock (this)
			{
				for (int i = 0; i < Count; i++)
				{
					yield return items[i];
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
