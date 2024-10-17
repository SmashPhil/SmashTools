using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SmashTools.Animations;
using UnityEngine;
using Verse;
using static SmashTools.Debug;

namespace SmashTools.Debugging
{
	internal class UnitTest_AnimDynamicMethod : UnitTest
	{
		private const int TestCount = 10;

		public override string Name => "AnimationProperty.DynamicMethod";

		public override TestType ExecuteOn => TestType.MainMenu;

		public override IEnumerable<UTResult> Execute()
		{
			TestObject testObject = new TestObject();

			yield return TestField(testObject, typeof(TestObject), nameof(TestObject.tInt));
			yield return TestField(testObject, typeof(TestObject), nameof(TestObject.tFloat));
			yield return TestField(testObject, typeof(TestObject), nameof(TestObject.tBool));

			FieldInfo vector3 = AccessTools.Field(typeof(TestObject), nameof(TestObject.vector));
			Assert(vector3 != null);
			yield return TestField(testObject, typeof(Vector3), nameof(Vector3.x), vector3);
			yield return TestField(testObject, typeof(Vector3), nameof(Vector3.y), vector3);
			yield return TestField(testObject, typeof(Vector3), nameof(Vector3.z), vector3);

			FieldInfo color = AccessTools.Field(typeof(TestObject), nameof(TestObject.color));
			Assert(color != null);
			yield return TestField(testObject, typeof(Color), nameof(Color.r), color);
			yield return TestField(testObject, typeof(Color), nameof(Color.g), color);
			yield return TestField(testObject, typeof(Color), nameof(Color.b), color);
			yield return TestField(testObject, typeof(Color), nameof(Color.a), color);

			FieldInfo intVec3 = AccessTools.Field(typeof(TestObject), nameof(TestObject.intVec3));
			Assert(intVec3 != null);
			yield return TestField(testObject, typeof(IntVec3), nameof(IntVec3.x), intVec3);
			yield return TestField(testObject, typeof(IntVec3), nameof(IntVec3.y), intVec3);
			yield return TestField(testObject, typeof(IntVec3), nameof(IntVec3.z), intVec3);
		}

		private UTResult TestField<T>(T obj, Type type, string name, params FieldInfo[] objectPath) where T : IAnimator
		{
			FieldInfo fieldInfo = AccessTools.Field(type, name);
			Assert(fieldInfo != null);
			AnimationProperty property = AnimationProperty.Create(typeof(T), name, fieldInfo, objectPath);
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
				property.Evaluate(obj, i);

				object container = obj;
				if (!objectPath.NullOrEmpty())
				{
					// Traverse down the object path to get to the property value
					foreach (FieldInfo path in objectPath)
					{
						container = path.GetValue(container);
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
				object container = obj;
				if (!objectPath.NullOrEmpty())
				{
					// Traverse down the object path to get to the property value
					foreach (FieldInfo path in objectPath)
					{
						container = path.GetValue(container);
					}
				}

				object value = fieldInfo.GetValue(container);
				float getValue = property.GetProperty(obj);
				if (!IsEqual(value, getValue))
				{
					return UTResult.For($"[GetValue] {type.Name}.{name}", false);
				}
				property.SetProperty(obj, 0);
				float setValue = property.GetProperty(obj);
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

			IEnumerable<IAnimationObject> IAnimator.ExtraAnimators => throw new NotImplementedException();

			Vector3 IAnimator.DrawPos => throw new NotImplementedException();

			string IAnimationObject.ObjectId => throw new NotImplementedException();
		}
	}
}
