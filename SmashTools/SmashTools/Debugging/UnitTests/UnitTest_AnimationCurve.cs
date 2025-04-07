using System;
using System.Collections.Generic;
using SmashTools.Animations;
using UnityEngine;
using Verse;

namespace SmashTools.UnitTesting;

internal class UnitTest_AnimationCurve : UnitTest
{
  public override string Name => "AnimationCurve";

  public override TestType ExecuteOn => TestType.Disabled;

  public override IEnumerable<UTResult> Execute()
  {
    var rwaCurve = new Animations.AnimationCurve();
    var unityCurve = new UnityEngine.AnimationCurve();
    rwaCurve.Add(0, 0);
    unityCurve.AddKey(new UnityEngine.Keyframe(0, 0));
    rwaCurve.Add(60, 1);
    unityCurve.AddKey(new UnityEngine.Keyframe(1, 1));
    yield return TestCurve("Default", rwaCurve, unityCurve);

    rwaCurve = new Animations.AnimationCurve();
    unityCurve = new UnityEngine.AnimationCurve();
    rwaCurve.Add(0, 0);
    unityCurve.AddKey(0, 0);
    rwaCurve.Add(60, 1);
    unityCurve.AddKey(new UnityEngine.Keyframe(1, 1));
    yield return TestCurve("Linear", rwaCurve, unityCurve);

    rwaCurve = new Animations.AnimationCurve();
    unityCurve = new UnityEngine.AnimationCurve();
    rwaCurve.Add(0, 0);
    unityCurve.AddKey(new UnityEngine.Keyframe(0, 0, 1, 1, 1, 1));
    rwaCurve.Add(60, 1);
    unityCurve.AddKey(new UnityEngine.Keyframe(1, 1));
    yield return TestCurve("Staircase", rwaCurve, unityCurve);
  }

  private UTResult TestCurve(string name, Animations.AnimationCurve rwaCurve,
    UnityEngine.AnimationCurve unityCurve)
  {
    return UTResult.For(name, true);
  }

  private bool IsEqual(object lhs, float rhs)
  {
    if (lhs is int i)
    {
      return i == (int)rhs;
    }
    if (lhs is float f)
    {
      return f == rhs;
    }
    if (lhs is bool b)
    {
      return b == (rhs > 0);
    }
    return false;
  }

  internal class TestObject : IAnimator
  {
    public int tInt;
    public float tFloat;
    public bool tBool;

    public Vector3 vector = new Vector3();
    public Color color = new Color();
    public IntVec3 intVec3 = new IntVec3();

    // None of these should be getting called, these are strictly for allowing this object to pass
    // as an IAnimator object to SetValue and GetValue delegates.

    AnimationManager IAnimator.Manager => throw new NotImplementedException();

    ModContentPack IAnimator.ModContentPack => throw new NotImplementedException();

    Vector3 IAnimator.DrawPos => throw new NotImplementedException();

    string IAnimationObject.ObjectId => throw new NotImplementedException();
  }
}