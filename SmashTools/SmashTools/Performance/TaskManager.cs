using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

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
				await Task.Run(action);
			}
			catch (Exception ex)
			{
				if (reportFailure)
				{
					string error = $"AsyncTask {action.Method.Name} threw exception while running. Exception = {ex}\n{StackTraceUtility.ExtractStringFromException(ex)}";
					Log.Error(error);
				}
			}
		}

		public static async Task<TResult> RunAsync<TResult>(Func<CancellationToken, TResult> action, CancellationToken? cancellationToken = null)
		{
			CancellationToken token = cancellationToken ?? CancellationToken.None;

			try
			{
				TResult result = await Task.Run(() =>
				{
					TResult returnValue = default;
					try
					{
						returnValue = action(token);
					}
					catch (Exception ex)
					{
						string reason = $"AsyncTask {action.Method.Name} threw exception while running. Exception = {ex}\n{StackTraceUtility.ExtractStringFromException(ex)}";
					}
					return returnValue;
				}, token);
				return result;
			}
			catch (Exception ex)
			{
				string error = $"AsyncTask {action.Method.Name} threw exception while running. Exception = {ex}\n{StackTraceUtility.ExtractStringFromException(ex)}";
				Log.Error(error);
			}
			return default(TResult);
		}
	}
}
