using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Verse;
using UnityEngine;

namespace SmashTools.Performance
{
	public static class AsyncPool<T> where T : new()
	{
		private static readonly ConcurrentBag<T> returnItems = new ConcurrentBag<T>();
		
		public static T Get()
		{
			if (returnItems.Count > 0 && returnItems.TryTake(out T item))
			{
				return item;
			}
			return Activator.CreateInstance<T>();
		}

		public static void Return(T item)
		{
			returnItems.Add(item);
		}
	}
}
