using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Rendering;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public readonly struct RenderData : IComparable<RenderData>
{
  internal readonly Rect rect;
  internal readonly Texture mainTex;
  internal readonly Material material;
  internal readonly MaterialPropertyBlock propertyBlock;
  internal readonly float layer;
  internal readonly float angle;

  public RenderData(Rect rect, Texture mainTex, Material material,
    MaterialPropertyBlock propertyBlock, float layer, float angle)
  {
    this.rect = rect;
    this.mainTex = mainTex;
    this.material = material;
    this.propertyBlock = propertyBlock;
    this.layer = layer;
    this.angle = angle;
  }

  public static RenderData Invalid => new(Rect.zero, null, null, null, -1, 0);

  int IComparable<RenderData>.CompareTo(RenderData other)
  {
    return layer.CompareTo(other.layer);
  }
}