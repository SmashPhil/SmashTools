using System.Diagnostics;
using Verse;

namespace SmashTools
{
	// Reimplementation of System.Diagnostics.Trace with IMGUI popup and RimWorld logger
	public static class Trace
	{
		[Conditional("TRACE")]
		public static void IsTrue(bool condition, string message = null)
		{
			if (condition) return;
			Raise(message);
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
#if DEBUG
			Assert.Raise(message);
#else
			Log.Error(message ?? "Assertion Failed");
#endif
		}
	}
}
