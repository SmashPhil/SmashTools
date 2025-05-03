using Verse;

namespace SmashTools.Rendering;

public interface IParallelRenderer
{
  void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData);
}