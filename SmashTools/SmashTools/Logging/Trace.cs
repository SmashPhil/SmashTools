using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace SmashTools
{
	// Reimplementation of System.Diagnostics.Trace with IMGUI popup and RimWorld logger
	public static class Trace
	{
		[Conditional("TRACE")]
		public static void IsTrue(bool condition)
		{
			if (condition) return;

			Log.Error($"Assertion Failed");
			if (Debugger.IsAttached) Debugger.Break();
			Debug.ShowStack("Assertion Failed");
		}

		[Conditional("TRACE")]
		public static void IsNull<T>(T obj, string message = null) where T : class
		{
			if (obj == null) return;
			Raise(message);
		}

		[Conditional("TRACE")]
		public static void IsNotNull<T>(T obj, string message = null) where T : class
		{
			if (obj != null) return;
			Raise(message);
		}

		[Conditional("TRACE")]
		public static void Raise(string message = null)
		{
			string readout = message != null ? $"Assertion Failed: {message}" : "Assertion Failed";
			Log.Error(readout);
		}
	}
}
