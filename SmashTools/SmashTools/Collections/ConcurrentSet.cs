using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SmashTools
{
	/// <summary>
	/// Wrapper collection for storing a hash set of items with shorthand methods for adding and removing items.
	/// </summary>
	/// <remarks>This is solely for convenience sake, and the value type of the dictionary is the smallest possible data type to avoid more overhead.</remarks>
	/// <typeparam name="T"></typeparam>
	public class ConcurrentSet<T> : ConcurrentDictionary<T, byte>
	{
		public bool Add(T item)
		{
			return TryAdd(item, 0);
		}

		public bool Remove(T item)
		{
			return TryRemove(item, out _);
		}

		public bool Contains(T item)
		{
			return ContainsKey(item);
		}
	}
}
