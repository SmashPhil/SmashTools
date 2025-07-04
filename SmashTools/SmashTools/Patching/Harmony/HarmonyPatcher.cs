using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Patching;

[PublicAPI]
public static class HarmonyPatcher
{
  private static Type typePatching;
  private static string methodPatching = string.Empty;

  private static readonly List<IPatchCategory> Patches = [];

  private static ModContentPack Mod { get; set; }

  internal static Harmony Harmony { get; private set; }

  private static bool RunningPatcher { get; set; }

  public static void Init(ModContentPack mod)
  {
    Mod = mod;
    Harmony = new Harmony(mod.ModMetaData.PackageIdPlayerFacing);
    foreach (Assembly assembly in mod.assemblies.loadedAssemblies)
    {
      //harmony.PatchAll(Assembly.GetExecutingAssembly());
      foreach (Type type in assembly.GetTypes())
      {
        if (type.HasInterface(typeof(IPatchCategory)) && type.IsClass && !type.IsAbstract)
        {
          IPatchCategory patch = (IPatchCategory)Activator.CreateInstance(type, nonPublic: true);
          Patches.Add(patch);
        }
      }
    }
  }

  public static void Run(PatchSequence sequence)
  {
    Harmony.DEBUG = true;

    Assert.IsFalse(RunningPatcher);

    using PatchStatusEnabler pse = new();

    DeepProfiler.Start($"HarmonyPatcher_{sequence}");
    foreach (IPatchCategory patch in Patches)
    {
      Assert.IsNotNull(patch);
      try
      {
        if (patch.PatchAt != sequence)
          continue;

        typePatching = patch.GetType();
        DeepProfiler.Start(typePatching.Name);
        patch.PatchMethods();
        DeepProfiler.End();
      }
      catch (Exception ex)
      {
        SmashLog.Error(
          $"Failed to Patch <type>{patch.GetType().FullName}</type>. Method=\"{methodPatching}\"\n{ex}");
      }
    }
    DeepProfiler.End();
  }

  public static void Patch(MethodBase original, HarmonyMethod prefix = null,
    HarmonyMethod postfix = null,
    HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
  {
    methodPatching = original?.Name ?? $"Null\", Previous=\"{methodPatching}";
    try
    {
      Harmony.Patch(original, prefix, postfix, transpiler, finalizer);
    }
    catch (Exception ex)
    {
      Log.Error($"Exception thrown patching {typePatching}::{methodPatching}.\n{ex}");
    }
  }

  [Conditional("DEBUG")]
  internal static void DumpPatchReport()
  {
    if (Prefs.DevMode)
    {
      int prefixes = 0;
      int postfixes = 0;
      int transpilers = 0;
      int finalizers = 0;
      List<MethodBase> patchMethods = Harmony.GetPatchedMethods().ToList();
      foreach (MethodBase method in patchMethods)
      {
        Patches patches = Harmony.GetPatchInfo(method);
        prefixes += CountPatches(patches.Prefixes);
        postfixes += CountPatches(patches.Postfixes);
        transpilers += CountPatches(patches.Transpilers);
        finalizers += CountPatches(patches.Finalizers);
      }
      SmashLog.Message(
        $"<color=orange>[{Mod.Name.Replace(" ", "")}]</color> <success>{prefixes + postfixes + transpilers + finalizers} " +
        $"patches successfully applied.</success>\nPrefixes: {prefixes} Postfixes: {postfixes} Transpilers: {transpilers} Finalizers: {finalizers}");
    }
    return;

    static int CountPatches(IReadOnlyCollection<Patch> patches)
    {
      int count = 0;
      foreach (Patch patch in patches)
      {
        if (patch.owner == Harmony.Id)
          count++;
      }
      return count;
    }
  }

  private readonly struct PatchStatusEnabler : IDisposable
  {
    public PatchStatusEnabler()
    {
      RunningPatcher = true;
    }

    void IDisposable.Dispose()
    {
      RunningPatcher = false;
      typePatching = null;
      methodPatching = null;
    }
  }
}