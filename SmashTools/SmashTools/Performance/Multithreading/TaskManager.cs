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
    public static async void RunAsync(Action action, Action<Exception> exceptionHandler)
    {
      try
      {
        await Task.Run(action).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        exceptionHandler(ex);
      }
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
          Log.Error($"AsyncTask {action.Method.Name} threw exception while running.\n{ex}");
        }
      }
    }
  }
}