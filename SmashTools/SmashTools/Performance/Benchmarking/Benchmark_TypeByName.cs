using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DevTools.Benchmarking;
using HarmonyLib;
using Verse;

// ReSharper disable all

namespace SmashTools.Performance;

[BenchmarkClass("TypeByName"), SampleSize(100)]
internal class Benchmark_TypeByName
{
  [Benchmark(Label = "AccessTools::TypeByName")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void TypeByName_AccessTools(ref TypeContext context)
  {
    foreach (string typeName in context.typesToFind)
    {
      _ = AccessTools.TypeByName(typeName);
    }
  }

  [Benchmark(Label = "ParseHelper::ParseType")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void TypeByName_ParseHelper(ref TypeContext context)
  {
    GenTypes.ClearCache();
    foreach (string typeName in context.typesToFind)
    {
      _ = ParseHelper.ParseType(typeName);
    }
  }

  [Benchmark(Label = "AccessTools::AllTypes().FirstOrDefault")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void TypeByName_AccessTools_AllTypes(ref TypeContext context)
  {
    foreach (string typeName in context.typesToFind)
    {
      Type type = AccessTools.AllTypes().FirstOrDefault((Type t) => t.FullName == typeName);
    }
  }

  [Benchmark(Label = "Assembly::GetType")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void TypeByName_Assembly_GetType(ref TypeContext context)
  {
    foreach (string typeName in context.typesToFind)
    {
      foreach (Assembly assembly in context.allAssemblies)
      {
        Type type = assembly.GetType(typeName, false, true);
        if (type != null)
        {
          break;
        }
      }
    }
  }

  private readonly struct TypeContext
  {
    public readonly IEnumerable<Assembly> allAssemblies;
    public readonly List<string> typesToFind;

    public TypeContext()
    {
      allAssemblies = (IEnumerable<Assembly>)AccessTools
       .PropertyGetter(typeof(GenTypes), "AllActiveAssemblies")
       .Invoke(null, []);

      typesToFind =
      [
        // Verse
        "Verse.GenTypes",
        // mscorlib
        "System.String",
        // Current executing assembly
        "SmashTools.Performance.Benchmark_TypeByName",
        // Harmony
        "HarmonyLib.AccessTools",
      ];
    }
  }
}