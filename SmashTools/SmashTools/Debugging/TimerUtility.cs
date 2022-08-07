using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Verse;

namespace SmashTools
{
	public static class TimerUtility
	{
		private static Stopwatch stopwatch;

		static TimerUtility()
		{
			stopwatch = new Stopwatch();
		}

		public static TimeSpan Run(Action action)
		{
			stopwatch.Reset();
			stopwatch.Start();
			action();
			stopwatch.Stop();

			return stopwatch.Elapsed;
		}
		
		public static void StartNew()
		{
			stopwatch.Reset();
			stopwatch.Start();
		}

		public static TimeSpan Stop()
		{
			stopwatch.Stop();
			return stopwatch.Elapsed;
		}
	}
}
