using Verse;

namespace SmashTools.Targeting;

public struct WorldTargetResult
{
  public required bool isValid;

  public TaggedString Tooltip { get; init; }

  public static WorldTargetResult Success => new() { isValid = true };

  public static WorldTargetResult Failed => new() { isValid = false };

  public static implicit operator bool(WorldTargetResult result)
  {
    return result.isValid;
  }
}