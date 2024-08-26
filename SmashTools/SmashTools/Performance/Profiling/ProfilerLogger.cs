using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Verse;

namespace SmashTools.Performance
{
	public delegate void LogMessageHandler(string message);
	public delegate void LogWarningHandler(string message);
	public delegate void LogErrorHandler(string message);

	public static class ProfilerLogger
	{
		public static void Link()
		{
			RegisterMessageLogger(LogMessage);
			RegisterWarningLogger(LogWarning);
			RegisterErrorLogger(LogError);
		}

		[DllImport("ClrProfiler.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void RegisterMessageLogger(LogMessageHandler logMessage);

		[DllImport("ClrProfiler.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void RegisterWarningLogger(LogWarningHandler logWarning);

		[DllImport("ClrProfiler.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void RegisterErrorLogger(LogErrorHandler logError);

		public static void LogMessage(string message) => Log.Message($"<color=gray>[ClrProfiler] {message}</color>");

		public static void LogWarning(string message) => Log.Warning($"<color=gray>[ClrProfiler] {message}</color>");

		public static void LogError(string message) => Log.Error($"<color=gray>[ClrProfiler] {message}</color>");
	}
}
