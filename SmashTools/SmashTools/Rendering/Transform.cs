using SmashTools.Animations;
using UnityEngine;
using Verse;

namespace SmashTools.Rendering;

public sealed class Transform : ITweakFields, IExposable
{
  [TweakField(SettingsType = UISettingsType.FloatBox)]
  [AnimationProperty(Name = "Position")]
  public Vector3 position;

  [TweakField(SettingsType = UISettingsType.SliderFloat)]
  [SliderValues(MinValue = -360, MaxValue = 360)]
  [AnimationProperty(Name = "Rotation")]
  public float rotation;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  [AnimationProperty(Name = "Scale")]
  public Vector3 scale;

  string ITweakFields.Category => null;

  string ITweakFields.Label => "Transform";

  void IExposable.ExposeData()
  {
    Scribe_Values.Look(ref position, nameof(position));
    Scribe_Values.Look(ref rotation, nameof(rotation));
    Scribe_Values.Look(ref scale, nameof(scale));
  }

  void ITweakFields.OnFieldChanged()
  {
  }
}