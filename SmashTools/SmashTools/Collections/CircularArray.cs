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
	public class CircularArray<T> : IEnumerable<T>
	{
		private readonly T[] array;
		private int head;
		private int tail;

		public CircularArray(int size)
		{
			array = new T[size];
		}

		public int Length => array.Length;

		public T[] InnerArray => array;

		public T this[int index]
		{
			get
			{
				int realIndex = GenMath.PositiveMod(tail + index, Length);
				return array[realIndex];
			}
		}

		/// <summary>
		/// Add item to array and increment internal index, dropping the previous item if the array was already populated at that index.
		/// </summary>
		/// <returns>Dropped item</returns>
		public T Push(T item)
		{
			T dropped = array[head];
			array[head] = item;

			head = GenMath.PositiveMod(++head, Length);
			if (head == tail)
			{
				tail = GenMath.PositiveMod(++tail, Length);
			}

			return dropped;
		}

		public void RemoveAt(int index)
		{
			int realIndex = GenMath.PositiveMod(tail + index, Length);
			array[realIndex] = default;
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = head; i != tail; i = GenMath.PositiveMod(++i, Length))
			{
				yield return array[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
