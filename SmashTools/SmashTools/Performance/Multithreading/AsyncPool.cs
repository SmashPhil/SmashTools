using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Verse;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using static SmashTools.Debug;

namespace SmashTools.Performance
{
	public static class AsyncPool<T> where T : class, new()
	{
		private static readonly ConcurrentBag<T> returnItems = new ConcurrentBag<T>();

		private static int bagLimit = int.MaxValue;

#if DEBUG
		/// <summary>
		/// Tracks the amount of objects created from AsyncPool retrievals and how many are returned.
		/// If more are returned than created, this will cause a memory leak.
		/// </summary>
		private static int counter = 0;
#endif

		public static int Count => returnItems.Count;

		public static T Get()
		{
			if (!returnItems.TryTake(out T item))
			{
				item = new T();
			}
			//Decrement even for new object instantiations,
			//this will allow it to even out to 0 when returned to bag
			ItemRemoved();
			return item;
		}

		public static void Return(T item)
		{
			if (Count == bagLimit) return;

			ItemReturned();
			returnItems.Add(item);
		}

		public static void SetLimit(int limit)
		{
			Interlocked.CompareExchange(ref bagLimit, limit, bagLimit);
		}

		[Conditional("DEBUG")]
		private static void ItemReturned()
		{
#if DEBUG
			Interlocked.Increment(ref counter);

			//More items have been borrowed than returned to bag.
			//Should this ever increment over 0, it means more objects
			//are being returned than retrieved. Aka Memory Leak
			Assert(counter <= 0);
#endif
		}

		[Conditional("DEBUG")]
		private static void ItemRemoved()
		{
#if DEBUG
			Interlocked.Decrement(ref counter);
#endif
		}
	}
}
