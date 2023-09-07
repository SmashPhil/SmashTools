using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
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
	}
}
