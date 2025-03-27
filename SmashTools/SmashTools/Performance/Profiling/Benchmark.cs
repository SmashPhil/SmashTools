using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using ThreadPriority = System.Threading.ThreadPriority;

namespace SmashTools.Performance
{
  /// <summary>
  /// Crude benchmarking util for comparing expensive operations.
  /// </summary>
  /// <remarks>
  /// Note: Only accurate if used for operations that are many times more 
  /// expensive than a single method call. This should not be used for 
  /// micro-benchmarking as results will not be accurate.
  /// </remarks>
  public static unsafe class Benchmark
  {
    public static void StartCheckingForCancellation(KeyCode keyCode, CancellationTokenSource cts)
    {
      CoroutineManager.Instance.StartCoroutine(InputLoop(keyCode, cts));
      return;

      static IEnumerator InputLoop(KeyCode keyCode, CancellationTokenSource cts)
      {
        while (!cts.IsCancellationRequested)
        {
          if (Input.GetKeyDown(keyCode))
          {
            SoundDefOf.Click.PlayOneShotOnCamera();
            cts.Cancel();
            yield break;
          }

          yield return null;
        }
      }
    }

    /// <returns>
    /// Time to run N <paramref name="iterations"/> of <paramref name="function"/> 
    /// </returns>
    /// <param name="iterations">Number of times to run this benchmark test.</param>
    /// <param name="function">Function to execute each iteration.</param>
    /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
    public static Results Run(int iterations, delegate*<void> function,
      Measurement measurement = Measurement.Milliseconds)
    {
#if !RELEASE
      Log.Warning(
        $"Benchmarks should be executed in Release to allow for JIT to make full optimizations.");
#endif

      if (Debugger.IsAttached)
      {
        Log.Error($@"Benchmarks should not be executed with debugger attached. 
The results will be wildly inaccurate.");
      }

      // We may be working on a low priority thread, set to normal priority
      // for standardized results.
      using NormalizeScheduling ns = new();

      // Ensure JIT has already ran for this code
      function();

      // Mitigate GC interference
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      Stopwatch watch;
      if (iterations >= 1000 && iterations % 10 == 0)
      {
        // Unroll loop for longer tests
        watch = Stopwatch.StartNew();
        for (int i = iterations; i > 0; i -= 10)
        {
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
        }

        watch.Stop();
      }
      else
      {
        // Run normally, presumably for testing expensive methods that won't
        // be ran thousands of times. Impact from loop will be low in this case.
        watch = Stopwatch.StartNew();
        for (int i = iterations; --i >= 0;)
        {
          function();
        }

        watch.Stop();
      }

      return new Results(watch, iterations, measurement);
    }

    /// <returns>
    /// Time to run N <paramref name="iterations"/> of <paramref name="function"/> 
    /// </returns>
    /// <param name="iterations">Number of times to run this benchmark test.</param>
    /// <param name="function">Function to execute each iteration.</param>
    /// <param name="context">Reference to object passed in with each function call.</param>
    /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
    public static Results Run<T>(int iterations, delegate*<ref T, void> function, ref T context,
      Measurement measurement = Measurement.Milliseconds)
    {
#if !RELEASE
      Log.Warning(
        $"Benchmarks should be executed in Release to allow for JIT to make full optimizations.");
#endif

      if (Debugger.IsAttached)
      {
        Log.Error($@"Benchmarks should not be executed with debugger attached. 
The results will be wildly inaccurate.");
      }

      // We may be working on a low priority thread, set to normal priority
      // for standardized results.
      using NormalizeScheduling ns = new();

      // Ensure JIT has already executed for this function
      function(ref context);

      // Mitigate GC interference
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      Stopwatch watch;
      if (iterations >= 1000 && iterations % 10 == 0)
      {
        // Unroll loop for longer tests
        watch = Stopwatch.StartNew();
        for (int i = iterations; i > 0; i -= 10)
        {
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
        }

        watch.Stop();
      }
      else
      {
        // Run normally, presumably for testing expensive methods that won't
        // be ran thousands of times. Impact from loop will be low in this case.
        watch = Stopwatch.StartNew();
        for (int i = iterations; --i >= 0;)
        {
          function(ref context);
        }

        watch.Stop();
      }

      return new Results(watch, iterations, measurement);
    }

    /// <returns>
    /// Time to run N <paramref name="iterations"/> of <paramref name="function"/> 
    /// </returns>
    /// <remarks>
    /// Uses delegate for benchmarking non-static functions or functions that need 
    /// to capture. This implementation will have lower accuracy due to the additional 
    /// cost of indirection and closures.
    /// </remarks>
    /// <param name="iterations">Number of times to run this benchmark test.</param>
    /// <param name="function">Function to execute each iteration.</param>
    /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
    public static Results Run(int iterations, Action function,
      Measurement measurement = Measurement.Milliseconds)
    {
#if !RELEASE
      Log.WarningOnce(
        "Benchmarks should be executed in Release to allow for JIT to make full optimizations.",
        typeof(Benchmark).GetHashCode());
#endif

      if (Debugger.IsAttached)
      {
        Log.Error(
          "Benchmarks should not be executed with debugger attached. The results will be inaccurate.");
      }

      // We may be working on a low priority thread, set to normal priority
      // for standardized results.
      using NormalizeScheduling ns = new();

      // Ensure JIT has already ran for this code
      function();

      // Mitigate GC interference
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      Stopwatch watch;
      if (iterations >= 1000)
      {
        // Unroll loop for longer tests
        watch = Stopwatch.StartNew();
        for (int i = iterations; i > 0; i -= 10)
        {
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
        }

        watch.Stop();
      }
      else
      {
        // Run normally, presumably for testing expensive methods that won't
        // be ran thousands of times. Impact from loop will be low in this case.
        watch = Stopwatch.StartNew();
        for (int i = iterations; --i >= 0;)
        {
          function();
        }

        watch.Stop();
      }

      return new Results(watch, iterations, measurement);
    }

    private static string MeasurementSuffix(Measurement measurement)
    {
      return measurement switch
      {
        Measurement.Seconds      => "s",
        Measurement.Milliseconds => "ms",
        Measurement.Microseconds => "us",
        Measurement.Nanoseconds  => "ns",
        _                        => throw new NotImplementedException(),
      };
    }

    public enum Measurement
    {
      Seconds,
      Milliseconds,
      Microseconds,
      Nanoseconds,
    }

    public readonly record struct Results
    {
      public readonly Measurement measurement;
      public readonly int iterations;

      public readonly double total;
      public readonly double mean;

      public Results(Stopwatch stopwatch, int iterations, Measurement measurement)
      {
        this.measurement = measurement;
        this.iterations = iterations;

        total = measurement switch
        {
          Measurement.Seconds           => stopwatch.Elapsed.TotalMilliseconds / 1000,
          Measurement.Microseconds      => stopwatch.Elapsed.Ticks * 1000,
          Measurement.Nanoseconds       => stopwatch.Elapsed.Ticks * 1000000,
          Measurement.Milliseconds or _ => stopwatch.Elapsed.TotalMilliseconds,
        };
        mean = total / iterations;
      }

      public string TotalString => $"{total:0.####} {MeasurementSuffix(measurement)}";

      public string MeanString => $"{mean:0.####} {MeasurementSuffix(measurement)}";

      public override string ToString()
      {
        return $@"Iterations={iterations} | Total={TotalString} | Mean={MeanString}";
      }
    }

    private readonly struct NormalizeScheduling : IDisposable
    {
      private readonly ProcessPriorityClass processPriority;
      private readonly ThreadPriority threadPriority;

      public NormalizeScheduling()
      {
        processPriority = Process.GetCurrentProcess().PriorityClass;
        threadPriority = Thread.CurrentThread.Priority;

        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
        Thread.CurrentThread.Priority = ThreadPriority.Normal;
      }

      void IDisposable.Dispose()
      {
        Process.GetCurrentProcess().PriorityClass = processPriority;
        Thread.CurrentThread.Priority = threadPriority;
      }
    }
  }
}