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
	public static class Trace
	{
		// Reimplementation of Trace.Assert with IMGUI popup
		[Conditional("TRACE")]
		public static void IsTrue(bool condition)
		{
			if (condition) return;

			Log.Error($"Assertion Failed");
			if (Debugger.IsAttached) Debugger.Break();
			Debug.ShowStack("Assertion Failed");
		}

		[Conditional("TRACE")]
		public static void IsTrue(bool condition, string message)
		{
			if (condition) return;

			Log.Error(message);
			if (Debugger.IsAttached) Debugger.Break();
			Debug.ShowStack(message);
		}
	}
}
