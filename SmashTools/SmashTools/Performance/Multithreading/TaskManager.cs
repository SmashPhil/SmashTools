using System;
using System.Threading.Tasks;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

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

  /// <summary>
  /// Run async action through a task as opposed to enqueueing on the dedicated thread.
  /// </summary>
  public static async void RunAsync(AsyncAction action, bool reportFailure = true)
  {
    try
    {
      Assert.IsTrue(action.IsValid);
      await Task.Run(action.Invoke).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      if (reportFailure)
      {
        Log.Error($"AsyncTask {action.GetType()} threw exception while running.\n{ex}");
        action.ExceptionThrown(ex);
      }
    }
  }
}