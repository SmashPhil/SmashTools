using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Performance;

[PublicAPI]
public readonly struct CollectionPool
{
	public static CollectionPoolScope<List<T>, T> GetList<T>(out List<T> list)
	{
		return new CollectionPoolScope<List<T>, T>(out list);
	}

	public static CollectionPoolScope<HashSet<T>, T> GetSet<T>(out HashSet<T> set)
	{
		return new CollectionPoolScope<HashSet<T>, T>(out set);
	}
}

[PublicAPI]
public readonly struct CollectionPoolScope<C, T> : IDisposable where C : ICollection<T>, new()
{
	private readonly C collection;

	public CollectionPoolScope(out C collection)
	{
		this.collection = SimplePool<C>.Get();
		// We still need to clear it before use since other sources can taint the contents of SimplePool.
		this.collection.Clear();
		collection = this.collection;
	}

	void IDisposable.Dispose()
	{
		collection.Clear();
		SimplePool<C>.Return(collection);
	}
}