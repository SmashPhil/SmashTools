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

namespace SmashTools.Debugging
{
	internal class UnitTest_AnimDynamicMethod : UnitTest
	{
		public override string Name => "AnimationProperty.DynamicMethod";

		public override IEnumerable<UTResult> Execute()
		{
			TestObject testObject = new TestObject();
			
			yield return TestField(testObject, nameof(TestObject.tInt));
			yield return TestField(testObject, nameof(TestObject.tFloat));
			yield return TestField(testObject, nameof(TestObject.tBool));

			Vector3 vector = new Vector3(0, 0, 0);
			//yield return TestField(vector, nameof(Vector3.x));
			//yield return TestField(vector, nameof(Vector3.y));
			//yield return TestField(vector, nameof(Vector3.z));

			//Rect rect = new Rect(0, 0, 0, 0);
			//yield return TestField(rect, nameof(Rect.x));
			//yield return TestField(rect, nameof(Rect.y));
			//yield return TestField(rect, nameof(Rect.width));
			//yield return TestField(rect, nameof(Rect.height));
		}

		private UTResult TestField<T>(T obj, string name)
		{
			FieldInfo fieldInfo = AccessTools.Field(typeof(T), name);
			Debug.Assert(fieldInfo != null);
			AnimationProperty property = AnimationProperty.Create(name, fieldInfo);
			
			(int frame, float value)[] results = {
				(0, 5),
				(1, 2),
				(2, 999)
			};
			for (int i = 0; i < results.Length; i++)
			{
				property.curve.Add(results[i].frame, results[i].value);
			}
			string label = string.Format("[{0}] {1}", typeof(T).Name, name);
			for (int i = 0; i < results.Length; i++)
			{
				// Only testing the actual KeyFrame values to make sure they are assigned
				// to the object. Any tests on curve evaluations should be done separately
				property.Set(ref obj, i);
				if (!IsEqual(fieldInfo.GetValue(obj), results[i].value))
				{
					return UTResult.For(label, false);
				}
			}
			return UTResult.For(label, true);
		}

		private bool IsEqual(object lhs, float rhs)
		{
			if (lhs is float f)
			{
				return f == rhs;
			}
			if (lhs is int i)
			{
				return i == (int)rhs;
			}
			if (lhs is bool b)
			{
				return b == (rhs > 0);
			}
			return false;
		}

		private class TestObject
		{
			public int tInt;
			public float tFloat;
			public bool tBool;
		}
	}
}
