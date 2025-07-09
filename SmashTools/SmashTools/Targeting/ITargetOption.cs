using JetBrains.Annotations;
using Verse;

namespace SmashTools.Targeting;

[PublicAPI]
public interface ITargetOption
{
  public TaggedString Label { get; }
}