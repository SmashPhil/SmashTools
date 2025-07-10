namespace SmashTools.Targeting;

public interface ITargeterSource<TTarget, in TPayload> where TPayload : ITargetOption
{
  bool TargeterValid { get; }

  /// <summary>
  /// Object can launch right now with its current configuration.
  /// </summary>
  TargetValidation CanTarget(TTarget target);

  /// <summary>
  /// Select target
  /// </summary>
  /// <param name="target"></param>
  /// <returns>Targeter can close and dequeue from the update loop.</returns>
  TargeterResult Select(TTarget target);

  /// <summary>
  /// Target selection confirmed
  /// </summary>
  void OnTargetingFinished(TargetData<TTarget> targetData, TPayload payload);
}