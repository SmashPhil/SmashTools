using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// Can be retrieved with map component indexing, but doesn't piggy back off <see cref="MapComponent"/>
/// and is not cached by Map.
/// </summary>
/// <remarks>
/// This is useful for behavior that only needs to exist alongside a map reference. ie. does not
/// need to tick, save, init on load, etc.
/// </remarks>
[PublicAPI]
public abstract class DetachedMapComponent
{
  protected readonly Map map;

  protected DetachedMapComponent(Map map)
  {
    this.map = map;
  }

  protected virtual void PreMapRemoval()
  {
  }
}