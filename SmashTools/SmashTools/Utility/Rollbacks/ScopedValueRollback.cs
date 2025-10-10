using System;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Records the current value of an object and rolls it back when this struct goes out of scope.
/// </summary>
/// <remarks>
/// This operates with the assumption that bdwgc remains non-compacting, and that the object in
/// <see cref="ptr"/> does not destroy before the rollback.
/// </remarks>
/// <typeparam name="T">Type of the object being rolled back.</typeparam>
[PublicAPI]
public readonly unsafe struct ScopedValueRollback<T> : IDisposable where T : unmanaged
{
	private readonly T* ptr;
	private readonly T value;

	/// <summary>
	/// Create temporary value state that rolls back to previous value when this struct goes out of scope.
	/// </summary>
	/// <param name="obj">object to rollback when this struct goes out of scope.</param>
	public ScopedValueRollback(ref T obj)
	{
		fixed (T* objPtr = &obj)
		{
			ptr = objPtr;
			value = obj;
		}
	}

	/// <summary>
	/// Create temporary value state that rolls back to previous value when this struct goes out of scope.
	/// </summary>
	/// <param name="obj">object to rollback when this struct goes out of scope.</param>
	/// <param name="value">Value to assign temporarily.</param>
	public ScopedValueRollback(ref T obj, T value)
	{
		fixed (T* objPtr = &obj)
		{
			ptr = objPtr;
			this.value = obj;
			obj = value;
		}
	}

	public void Dispose()
	{
		*ptr = value;
	}
}