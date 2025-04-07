using DevTools;
using UnityEngine;
using Verse;

namespace SmashTools
{
  public readonly record struct TransformData
  {
    public readonly Vector3 position;
    public readonly Rot8 orientation;
    public readonly float rotation;

    public TransformData()
    {
      this.position = Vector3.zero;
      this.orientation = Rot8.Invalid;
      this.rotation = 0;
    }

    public TransformData(Vector3 position)
    {
      this.position = position;
    }

    public TransformData(Vector3 position, Rot8 orientation) : this(position)
    {
      this.position = position;
      this.orientation = orientation;
    }

    public TransformData(Vector3 position, Rot8 orientation, float rotation)
      : this(position, orientation)
    {
      this.rotation = rotation;
    }

    // NOTE - Orientation is not added or modified. The original orientation will be copied
    // over for any arithmetic operations.
    public TransformData Add(Vector3 position, float rotation)
    {
      return new TransformData(this.position + position, orientation, this.rotation + rotation);
    }

    public static TransformData For(Thing thing, Rot8? rot = null, float? extraRotation = null)
    {
      return new TransformData(thing.DrawPos, rot ?? thing.Rotation, extraRotation ?? 0);
    }

    // Transforms should only be additive for hierarchal values. Other arithmetic operators are not necessary
    public static TransformData operator +(TransformData lhs, TransformData rhs)
    {
      Assert.IsTrue(lhs.orientation == rhs.orientation,
        @"Mismatched orientations for TransformData. 
2nd transform orientation will be overridden.");
      return new TransformData(lhs.position + rhs.position, lhs.orientation,
        lhs.rotation + rhs.rotation);
    }
  }
}