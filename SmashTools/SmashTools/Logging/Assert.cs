using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public static class Assert
	{
		[Conditional("DEBUG")]
		public static void IsTrue(bool condition)
		{
			IsTrue(condition, null);
		}

		[Conditional("DEBUG")]
		public static void IsTrue(bool condition, string message)
		{
			if (condition) return;
			Raise(message);
		}

		[Conditional("DEBUG")]
		public static void IsNull<T>(T obj) where T : class
		{
			IsNull(obj, null);
		}

		[Conditional("DEBUG")]
		public static void IsNull<T>(T obj, string message) where T : class
		{
			if (obj == null) return;
			Raise(message);
		}

		[Conditional("DEBUG")]
		public static void IsNotNull<T>(T obj) where T : class
		{
			IsNotNull(obj, null);
		}

		[Conditional("DEBUG")]
		public static void IsNotNull<T>(T obj, string message) where T : class
		{
			if (obj != null) return;
			Raise(message);
		}

		[Conditional("DEBUG")]
		public static void Raise()
		{
			Raise(null);
		}

		[Conditional("DEBUG")]
		public static void Raise(string message)
		{
			string readout = message != null ? $"Assertion Failed: {message}" : "Assertion Failed";
			Log.Error(readout);
			if (Debugger.IsAttached) Debugger.Break();
			Debug.ShowStack(readout);
		}
	}
}
