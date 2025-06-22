using System;
using Verse;
using HarmonyLib;
using JetBrains.Annotations;

namespace SmashTools.Patching;

[PublicAPI]
public abstract class ConditionalPatch
{
  /// <summary>
  /// Patch implementations.  Only supports manual patches
  /// </summary>
  /// <param name="mod"></param>
  /// <param name="instance"></param>
  public abstract void PatchAll(ModMetaData mod, Harmony instance);

  /// <summary>
  /// Mod implementing this conditional patch
  /// </summary>
  /// <remarks>For debugging purposes</remarks>
  public abstract string SourceId { get; }

  /// <summary>
  /// Mod this patch will be applied if active in the mod list
  /// </summary>
  public abstract string PackageId { get; }

  [PublicAPI]
  public class Result
  {
    public string PackageId { get; set; }
    public string FriendlyName { get; set; }
    public bool Active { get; set; }
    public Exception ExceptionThrown { get; set; }
  }
}