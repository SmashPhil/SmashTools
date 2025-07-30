using System;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Performance;

/// <summary>
/// Schedules an action to be executed after a specified delay, resetting the delay if invoked again before expiration.
/// </summary>
[PublicAPI]
public class Debouncer : IDisposable
{
  private readonly float timeDelay; // seconds
  private readonly TimerImpl timer;

  private CancellationTokenSource cts;

  /// <summary>
  /// Initializes a new <see cref="Debouncer"/> action.
  /// </summary>
  /// <param name="action">The action to execute after the delay.</param>
  /// <param name="milliseconds">The delay duration in milliseconds.</param>
  public Debouncer(Action action, int milliseconds)
  {
    timeDelay = milliseconds / 1000f;

    cts = new CancellationTokenSource();
    timer = new TimerImpl(action, cts.Token);
    timer.Reset(timeDelay);
    UnityThread.StartUpdate(timer.ContinueUpdate);
  }

  /// <summary>
  /// Resets the debounce timer. The action will be delayed again.
  /// </summary>
  public void Invoke()
  {
    cts ??= new CancellationTokenSource();
    timer.Reset(timeDelay);
  }

  /// <summary>
  /// Cancels the scheduled action if it hasn't executed yet.
  /// </summary>
  public void Cancel()
  {
    cts?.Cancel();
    cts = null;
  }

  /// <summary>
  /// Releases resources used by the <see cref="Debouncer"/>.
  /// </summary>
  void IDisposable.Dispose()
  {
    cts?.Dispose();
  }

  /// <summary>
  /// Internal timer that tracks elapsed time and executes the action once expired.
  /// </summary>
  private class TimerImpl(Action action, in CancellationToken token)
  {
    private readonly CancellationToken token = token;
    private float timeLeft;

    /// <summary>
    /// Indicates whether the timer has expired.
    /// </summary>
    private bool Expired => timeLeft <= 0;

    /// <summary>
    /// Resets the internal timer to the specified delay.
    /// </summary>
    /// <param name="timeDelay">The new delay in seconds.</param>
    public void Reset(float timeDelay)
    {
      timeLeft = timeDelay;
    }

    /// <summary>
    /// Advances the timer and triggers the action if expired.
    /// </summary>
    /// <remarks>Enqueued as <see cref="UnityThread.OnUpdate"/> and called as part of the <see cref="UnityThread.Update"/> loop.</remarks>
    /// <returns>True if the timer is still active; otherwise, false.</returns>
    public bool ContinueUpdate()
    {
      if (token.IsCancellationRequested)
        return false;

      timeLeft -= Time.deltaTime;
      if (Expired)
      {
        action();
        return false;
      }
      return true;
    }
  }
}