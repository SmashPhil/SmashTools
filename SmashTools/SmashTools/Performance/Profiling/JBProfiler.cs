//#define PROFILER

#if PROFILER
using System.Diagnostics;
using JetBrains;
using JetBrains.Profiler.Api;
using JetBrains.Profiler.SelfApi;
using static SmashTools.Debug;

namespace SmashTools.Performance.JetBrains
{
	internal static class JBProfiler
	{
		static JBProfiler()
		{
#if RELEASE
			Log.Error($"Calling DotTrace on release build!");
#endif
		}

		[Conditional("DEBUG")]
		public static void StartCollectingData(string groupName = null)
		{
			MeasureProfiler.StartCollectingData(groupName: groupName);
		}

		[Conditional("DEBUG")]
		public static void StopCollectingData()
		{
			MeasureProfiler.StopCollectingData();
		}

		[Conditional("DEBUG")]
		public static void SaveData(string name = null)
		{
			MeasureProfiler.SaveData(name);
		}

		[Conditional("DEBUG")]
		public static void DropData()
		{
			MeasureProfiler.DropData();
		}
	}
}
#endif