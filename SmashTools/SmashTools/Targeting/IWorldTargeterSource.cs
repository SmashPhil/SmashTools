using RimWorld.Planet;

namespace SmashTools.Targeting;

public interface IWorldTargeterSource<in TPayload> where TPayload : ITargetOption
{
  /// <summary>
  /// Object can launch right now with its current configuration.
  /// </summary>
  WorldTargetResult CanTarget(GlobalTargetInfo target);

  /// <summary>
  /// Select target
  /// </summary>
  /// <param name="target"></param>
  /// <returns>Targeter can close and dequeue from the update loop.</returns>
  TargeterResult Select(GlobalTargetInfo target);

  /// <summary>
  /// Target selection confirmed
  /// </summary>
  void OnTargetingFinished(TargetData<GlobalTargetInfo> targetData, TPayload payload);
}