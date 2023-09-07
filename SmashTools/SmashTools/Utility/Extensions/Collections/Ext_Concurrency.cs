using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public static class Ext_Concurrency
	{
		public static bool NullOrEmpty<T>(this ConcurrentBag<T> bag)
		{
			return bag is null || bag.Count == 0;
		}

		public static bool NullOrEmpty<T>(this ConcurrentSet<T> set)
		{
			return set is null || set.Count == 0;
		}
	}
}
