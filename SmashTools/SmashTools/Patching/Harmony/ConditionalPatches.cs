using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Patching;

[PublicAPI]
public static class ConditionalPatches
{
	private static readonly Dictionary<string, List<IConditionalPatch.Result>> PatchResults = [];

	private static readonly Dictionary<PatchSequence, List<IConditionalPatch>> Patches = [];

	internal static void Init(ModContentPack mod)
	{
		foreach (Type type in mod.AllInterfaceClassImplementations<IConditionalPatch>())
		{
			IConditionalPatch patch = (IConditionalPatch)Activator.CreateInstance(type, null);
			Patches.AddOrAppend(patch.PatchAt, patch);
		}
	}

	/// <summary>
	/// Apply all conditional patches for a mod
	/// </summary>
	internal static void Run(PatchSequence sequence)
	{
		if (!Patches.TryGetValue(sequence, out List<IConditionalPatch> patches))
			return;

		foreach (IConditionalPatch patch in patches)
		{
			IConditionalPatch.Result result = new()
			{
				PackageId = patch.PackageId
			};
			if (Ext_Mods.GetActiveMod(patch.PackageId) is { } modMetaData)
			{
				PatchResults.AddOrAppend(patch.SourceId, result);
				try
				{
					result.FriendlyName = modMetaData.Name;
					patch.PatchAll(modMetaData);
					result.Active = true;
				}
				catch (Exception ex)
				{
					Log.Error($"{ProjectSetup.LogLabel} Failed to apply compatibility patch {result.FriendlyName}.\n{ex}");
					result.Active = false;
					result.ExceptionThrown = ex;
				}
			}
		}
	}

	public static List<IConditionalPatch.Result> GetPatches(string sourceId)
	{
		return PatchResults.TryGetValue(sourceId);
	}

	public static void DumpPatchReport()
	{
		StringBuilder reportBuilder = new();
		foreach ((string sourceId, List<IConditionalPatch.Result> patches) in PatchResults)
		{
			if (patches.NullOrEmpty())
				continue;

			foreach (IConditionalPatch.Result result in patches)
			{
				reportBuilder.AppendLine(
					$"[{sourceId}] Applying compatibility patch for {result.PackageId}. Active: {result.Active.ToStringYesNo()}");
			}
		}
		if (reportBuilder.Length > 0)
			Log.Message(reportBuilder.ToString().TrimEnd());
	}
}