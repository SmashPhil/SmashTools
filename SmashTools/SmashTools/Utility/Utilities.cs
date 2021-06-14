using System;
using Verse;

namespace SmashTools
{
	public static class Utilities
	{
		/// <summary>
		/// Action delegates with pass-by-reference parameters allowed
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item"></param>
		public delegate void ActionRef<T>(ref T item);
		public delegate void ActionRefP1<T1, T2>(ref T1 item1, T2 item2);
		public delegate void ActionRefP2<T1, T2>(T1 item1, ref T2 item2);
		public delegate void ActionRef<T1, T2>(ref T1 item1, ref T2 item2);

		/// <summary>
		/// <paramref name="source"/> is the same Type as or derived from <paramref name="target"/>
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		public static bool SameOrSubclass(this Type source, Type target)
		{
			return source == target || source.IsSubclassOf(target);
		}
	}
}
