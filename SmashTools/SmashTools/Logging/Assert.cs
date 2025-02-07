using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace SmashTools
{
	// Reimplementation of System.Diagnostics.Assert with IMGUI popup and RimWorld logger
	public static class Assert
	{
		[Conditional("DEBUG")]
		public static void IsTrue(bool condition, string message = null)
		{
			if (condition) return;
			Raise(message);
		}

		[Conditional("DEBUG")]
		public static void IsNull<T>(T obj, string message = null) where T : class
		{
			if (obj == null) return;
			Raise(message);
		}

		[Conditional("DEBUG")]
		public static void IsNotNull<T>(T obj, string message = null) where T : class
		{
			if (obj != null) return;
			Raise(message);
		}

		[Conditional("DEBUG")]
		public static void Raise(string message = null)
		{
			string readout = message != null ? $"Assertion Failed: {message}" : "Assertion Failed";
			Log.Error(readout);
			if (Debugger.IsAttached) Debugger.Break();
			Debug.ShowStack(readout);
		}
	}
}
