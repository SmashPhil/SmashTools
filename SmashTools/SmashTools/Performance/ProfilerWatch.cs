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
		private static Dictionary<string, Stopwatch> stopwatch = new Dictionary<string, Stopwatch>();

		private static object profilerLock = new object(); //TODO - implement concurrency to avoid overlapping reading / writing to dictionary from same caller

		public static void Start(string caller)
		{
			stopwatch[caller] = new Stopwatch();
			stopwatch[caller].Restart();
		}

		public static void Post(string caller)
		{
			stopwatch[caller].Stop();
			TimeSpan span = stopwatch[caller].Elapsed;
			Log.Message($"{caller}: {span.TotalMilliseconds:0.000}ms");
			stopwatch[caller].Restart();
		}

		public static void End(string caller)
		{
			stopwatch[caller].Stop();
			stopwatch.Remove(caller);
		}
	}
}
