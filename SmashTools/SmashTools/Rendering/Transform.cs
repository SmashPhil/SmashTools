using SmashTools.Animations;
using UnityEngine;

namespace SmashTools.Rendering;

public sealed class Transform
{
  [AnimationProperty(Name = "Position")]
  public Vector3 position;

  [AnimationProperty(Name = "Rotation")]
  public float rotation;

  [AnimationProperty(Name = "Scale")]
  public Vector3 scale;
}