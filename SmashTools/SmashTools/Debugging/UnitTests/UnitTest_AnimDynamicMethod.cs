using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools;
using HarmonyLib;
using SmashTools.Animations;
using UnityEngine;
using Verse;

namespace SmashTools.UnitTesting
{
  internal class UnitTest_AnimDynamicMethod : UnitTest
  {
    private const int TestCount = 10;

    public override string Name => "AnimationDynamicMethod";

    public override TestType ExecuteOn => TestType.MainMenu;

    public override IEnumerable<UTResult> Execute()
    {
      TestObject testObject = new();

      yield return TestField(testObject, typeof(TestObject), nameof(TestObject.tInt), null);
      yield return TestField(testObject, typeof(TestObject), nameof(TestObject.tFloat), null);
      yield return TestField(testObject, typeof(TestObject), nameof(TestObject.tBool), null);

      FieldInfo vector3Field = AccessTools.Field(typeof(TestObject), nameof(TestObject.vector));
      ObjectPath vector3Path = new(vector3Field);
      Assert.IsNotNull(vector3Field);
      yield return TestField(testObject, typeof(Vector3), nameof(Vector3.x), vector3Path);
      yield return TestField(testObject, typeof(Vector3), nameof(Vector3.y), vector3Path);
      yield return TestField(testObject, typeof(Vector3), nameof(Vector3.z), vector3Path);

      FieldInfo colorField = AccessTools.Field(typeof(TestObject), nameof(TestObject.color));
      ObjectPath colorPath = new(colorField);
      Assert.IsNotNull(colorField);
      yield return TestField(testObject, typeof(Color), nameof(Color.r), colorPath);
      yield return TestField(testObject, typeof(Color), nameof(Color.g), colorPath);
      yield return TestField(testObject, typeof(Color), nameof(Color.b), colorPath);
      yield return TestField(testObject, typeof(Color), nameof(Color.a), colorPath);

      FieldInfo intVec3Field = AccessTools.Field(typeof(TestObject), nameof(TestObject.intVec3));
      ObjectPath intVec3Path = new(intVec3Field);
      Assert.IsNotNull(intVec3Field);
      yield return TestField(testObject, typeof(IntVec3), nameof(IntVec3.x), intVec3Path);
      yield return TestField(testObject, typeof(IntVec3), nameof(IntVec3.y), intVec3Path);
      yield return TestField(testObject, typeof(IntVec3), nameof(IntVec3.z), intVec3Path);

      FieldInfo nestedObjField =
        AccessTools.Field(typeof(TestObject), nameof(TestObject.nestedObject));
      ObjectPath nestedObjPath = new(nestedObjField);
      Assert.IsNotNull(intVec3Field);
      yield return TestField(testObject, typeof(NestedTestObject), nameof(NestedTestObject.tInt),
        nestedObjPath);
      yield return TestField(testObject, typeof(NestedTestObject), nameof(NestedTestObject.tFloat),
        nestedObjPath);
      yield return TestField(testObject, typeof(NestedTestObject), nameof(NestedTestObject.tBool),
        nestedObjPath);
    }

    private UTResult TestField(IAnimator animator, Type type, string name,
      params ObjectPath[] objectPath)
    {
      FieldInfo fieldInfo = AccessTools.Field(type, name);
      Type objType = animator.GetType();
      // Hierarchy and Object paths are the same since TestObject has no depth
      AnimationProperty property =
        AnimationProperty.Create(objType, name, fieldInfo, objectPath?.LastOrDefault());
      (int frame, float value)[] results = new (int frame, float value)[TestCount];
      for (int i = 0; i < results.Length; i++)
      {
        float value = Rand.Range(0, 99999);
        results[i] = (i, value);
        property.curve.Add(i, value);
      }
      // Curve evaluation
      for (int i = 0; i < results.Length; i++)
      {
        // Only testing the actual KeyFrame values to make sure they are assigned
        // to the object. Any tests on curve evaluations should be done separately
        property.Evaluate(animator, i);

        object container = animator;
        if (!objectPath.NullOrEmpty())
        {
          foreach (ObjectPath path in objectPath)
          {
            // Traverse down the object path to get to the property value
            container = path.FieldInfo.GetValue(container);
          }
        }

        object value = fieldInfo.GetValue(container);
        if (!IsEqual(value, results[i].value))
        {
          return UTResult.For($"[Evaluate] {type.Name}.{name}", false);
        }
      }
      // Set / Get
      {
        object container = animator;
        if (!objectPath.NullOrEmpty())
        {
          foreach (ObjectPath path in objectPath)
          {
            // Traverse down the object path to get to the property value
            container = path.FieldInfo.GetValue(container);
          }
        }

        object value = fieldInfo.GetValue(container);
        float getValue = property.GetProperty(animator);
        if (!IsEqual(value, getValue))
        {
          return UTResult.For($"[GetValue] {type.Name}.{name}", false);
        }
        property.SetProperty(animator, 0);
        float setValue = property.GetProperty(animator);
        if (setValue != 0f)
        {
          return UTResult.For($"[SetValue] {type.Name}.{name}", false);
        }
      }
      return UTResult.For($"{type.Name}.{name}", true);
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

    private class TestObject : IAnimator
    {
      public int tInt;
      public float tFloat;
      public bool tBool;

      public Vector3 vector = new();
      public Color color = new();
      public IntVec3 intVec3 = new();

      public NestedTestObject nestedObject = new();

      public List<NestedTestObject> nestedObjectList = [];

      // None of these should be getting called, these are strictly for allowing this object to pass
      // as an IAnimator object to SetValue and GetValue delegates.

      AnimationManager IAnimator.Manager => throw new NotImplementedException();

      ModContentPack IAnimator.ModContentPack => throw new NotImplementedException();

      Vector3 IAnimator.DrawPos => throw new NotImplementedException();

      string IAnimationObject.ObjectId => throw new NotImplementedException();
    }

    private class NestedTestObject : IAnimationObject
    {
      public int tInt = 0;
      public float tFloat = 0;
      public float tBool = 0;

      public string ObjectId => throw new NotImplementedException();
    }
  }
}