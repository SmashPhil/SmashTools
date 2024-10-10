using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains;
using JetBrains.Profiler.Api;
using Verse;
using static SmashTools.Debug;

namespace SmashTools.Performance.JetBrains
{
	internal static class DotTrace
	{
		static DotTrace()
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
