using System.Diagnostics;
using Verse;

namespace SmashTools
{
	public static class Debug
	{
		[Conditional("DEBUG")]
		public static void Assert(bool condition)
		{
			if (condition) return;

			Log.Error("Assertion Failed");
			if (Debugger.IsAttached) Debugger.Break();
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message)
		{
			if (condition) return;

			Log.Error($"Assertion Failed: {message}");
			if (Debugger.IsAttached) Debugger.Break();
		}

		/// <summary>
		/// Thread Safe message logging, passes to coroutine to invoke on main thread in bulk.
		/// </summary>
		public static void TSMessage(string message)
		{
			CoroutineManager.QueueInvoke(() => Log.Message(message));
		}

		/// <summary>
		/// Thread Safe warning logging, passes to coroutine to invoke on main thread in bulk.
		/// </summary>
		public static void TSWarning(string message)
		{
			CoroutineManager.QueueInvoke(() => Log.Warning(message));
		}

		/// <summary>
		/// Thread Safe error logging, passes to coroutine to invoke on main thread in bulk.
		/// </summary>
		public static void TSError(string message)
		{
			CoroutineManager.QueueInvoke(() => Log.Error(message));
		}
	}
}
