using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

[PublicAPI]
[StaticConstructorOnStartup]
public static class ConditionalPatches
{
  private static readonly Dictionary<Type, ConditionalPatch.Results> patches = [];

  /// <summary>
  /// Apply all conditional patches for a mod
  /// </summary>
  static ConditionalPatches()
  {
    Harmony harmony = ProjectSetup.Harmony;

    List<Type> conditionalPatchTypes = typeof(ConditionalPatch).AllSubclassesNonAbstract();
    foreach (Type type in conditionalPatchTypes)
    {
      try
      {
        ConditionalPatch patch = (ConditionalPatch)Activator.CreateInstance(type, null);
        ConditionalPatch.Results result = new()
        {
          PackageId = patch.PackageId
        };
        if (ModLister.GetActiveModWithIdentifier(patch.PackageId, ignorePostfix: true) is
          { } modMetaData)
        {
          result.FriendlyName = modMetaData.Name;

          patch.PatchAll(modMetaData, harmony);

          result.Active = true;

          Log.Message(
            $"[{patch.SourceId}] Successfully applied compatibility patches for {modMetaData.Name}");
        }
        patches[type] = result;
      }
      catch (Exception ex)
      {
        Log.Error($"{ProjectSetup.LogLabel} Failed to apply patch {type}.\n{ex}");
      }
    }
  }

  public static ConditionalPatch.Results PatchResult<T>() where T : ConditionalPatch
  {
    return patches.TryGetValue(typeof(T), ConditionalPatch.Results.Invalid);
  }

  public static bool PatchIsActive<T>() where T : ConditionalPatch
  {
    return PatchResult<T>().Active;
  }
}