using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using System.Linq;

namespace SmashTools
{
	public static class TaskManager
	{
		private static int previousTick = -1;

		public static void SleepTillNextTick()
		{
			previousTick = Find.TickManager.TicksGame;

			//Needs null check if quitting while Task is still running
			while (Find.TickManager != null && previousTick == Find.TickManager.TicksGame);
		}

		public static async void RunAsync(Action action, bool reportFailure = true)
		{
			try
			{
				await Task.Run(action).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (reportFailure)
				{
					Log.Error($"AsyncTask {action.Method.Name} threw exception while running. Exception = {ex}");
				}
			}
		}
	}
}
