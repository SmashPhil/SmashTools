using JetBrains.Annotations;

namespace SmashTools.Performance;

[PublicAPI]
public interface IPoolable
{
	/// <summary>
	/// Object is currently in the object pool
	/// </summary>
	bool InPool { get; set; }

	/// <summary>
	/// Clear all references as object is being returned to pool
	/// </summary>
	public void Reset();
}