using System.Diagnostics;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public static class Debug
	{
		private static readonly Vector2 popupSize = new Vector2(500, 650);

		[Conditional("DEBUG")]
		public static void Assert(bool condition)
		{
			if (condition) return;

			Log.Error("Assertion Failed");
			if (Debugger.IsAttached) Debugger.Break();
			Find.WindowStack.Add(new Dialog_Popup(popupSize, "Assertion Failed", StackTraceUtility.ExtractStackTrace()));
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message)
		{
			if (condition) return;

			Log.Error($"Assertion Failed: {message}");
			if (Debugger.IsAttached) Debugger.Break();
			Find.WindowStack.Add(new Dialog_Popup(popupSize, message, StackTraceUtility.ExtractStackTrace()));
		}

		// Reimplementation of Trace.Assert with IMGUI popup
		[Conditional("TRACE")]
		public static void Trace(bool condition)
		{
			if (condition) return;

			Log.Error($"Assertion Failed");
			if (Debugger.IsAttached) Debugger.Break();
			Find.WindowStack.Add(new Dialog_Popup(popupSize, "Assertion Failed", StackTraceUtility.ExtractStackTrace()));
		}

		[Conditional("TRACE")]
		public static void Trace(bool condition, string message)
		{
			if (condition) return;

			Log.Error(message);
			if (Debugger.IsAttached) Debugger.Break();
			Find.WindowStack.Add(new Dialog_Popup(popupSize, message, StackTraceUtility.ExtractStackTrace()));
		}

		/// <summary>
		/// Thread Safe message logging, passes to coroutine to invoke on main thread in bulk.
		/// </summary>
		public static void TSMessage(string message)
		{
			if (UnityData.IsInMainThread)
			{
				Log.Message(message);
			}
			else
			{
				CoroutineManager.QueueInvoke(() => Log.Message(message));
			}
		}

		/// <summary>
		/// Thread Safe warning logging, passes to coroutine to invoke on main thread in bulk.
		/// </summary>
		public static void TSWarning(string message)
		{
			if (UnityData.IsInMainThread)
			{
				Log.Warning(message);
			}
			else
			{
				CoroutineManager.QueueInvoke(() => Log.Warning(message));
			}
		}

		/// <summary>
		/// Thread Safe error logging, passes to coroutine to invoke on main thread in bulk.
		/// </summary>
		public static void TSError(string message)
		{
			if (UnityData.IsInMainThread)
			{
				Log.Error(message);
			}
			else
			{
				CoroutineManager.QueueInvoke(() => Log.Error(message));
			}
		}
	}
}
