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

  private static readonly List<IPatchCategory> patches = [];

#if DEBUG
  private static readonly Dictionary<PatchSequence, int> patchCounts = [];
#endif

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
          patches.Add(patch);
        }
      }
    }
  }

  public static void Run(PatchSequence sequence)
  {
    Assert.IsFalse(RunningPatcher);

    using PatchStatusEnabler pse = new();

    //Harmony.DEBUG = true;
    foreach (IPatchCategory patch in patches)
    {
      Assert.IsNotNull(patch);
      try
      {
        if (patch.PatchAt != sequence)
          continue;

        typePatching = patch.GetType();
        patch.PatchMethods();
      }
      catch (Exception ex)
      {
        SmashLog.Error(
          $"Failed to Patch <type>{patch.GetType().FullName}</type>. Method=\"{methodPatching}\"\n{ex}");
      }
    }
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
      SmashLog.Message(
        $"<color=orange>[{Mod.Name.Replace(" ", "")}]</color> <success>{Harmony.GetPatchedMethods().Count()} " +
        $"patches successfully applied.</success>");
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