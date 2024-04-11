using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Performance
{
	public static class ProfilerWatch
	{
		private static Stopwatch stopwatch = new Stopwatch();

		public static bool Start()
		{
			if (stopwatch.IsRunning)
			{
				Log.Error($"Attempting to start stopwatch twice.");
				return false;
			}
			stopwatch.Start();
			return true;
		}

		public static TimeSpan Get()
		{
			stopwatch.Stop();
			TimeSpan span = stopwatch.Elapsed;
			stopwatch.Restart();
			return span;
		}

		public static void Restart()
		{
			stopwatch.Restart();
		}

		public static TimeSpan Post(string label)
		{
			stopwatch.Stop();
			TimeSpan span = stopwatch.Elapsed;
			Log.Message($"{label}: {span.TotalMilliseconds:0.0000}ms");
			stopwatch.Restart();
			return span;
		}

		public static void End()
		{
			stopwatch.Stop();
		}
	}
}
