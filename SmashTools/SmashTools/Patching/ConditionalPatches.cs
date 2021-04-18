using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace SmashTools
{
	public static class ConditionalPatches
	{
		/// <summary>
		/// <c>Key</c>: (source mod applying patch, package ID of mod being patched)
		/// </summary>
		public static Dictionary<Pair<string, string>, ModPatchable> patchableModActivators = new Dictionary<Pair<string, string>, ModPatchable>();

		/// <summary>
		/// Apply all conditional patches for a mod
		/// </summary>
		/// <param name="harmony"></param>
		/// <param name="sourcePackageID"></param>
		public static (ModMetaData metaData, ModContentPack modContent) PatchAllActiveMods(Harmony harmony, string sourcePackageID)
		{
			IEnumerable<ModMetaData> mods = ModLister.AllInstalledMods.Where(m => m.Active);
			IEnumerable<Type> interfaceImplementations = GenTypes.AllTypes.Where(t => t.GetInterfaces().Contains(typeof(IConditionalPatch)));

			//Double looping is fine in this case since I want the matching mod to be captured before looping through the entire mod list
			ModMetaData modMetaData = mods.FirstOrDefault(m => m.PackageId.EqualsIgnoreCase(sourcePackageID) || m.PackageId.Contains(sourcePackageID));
			ModContentPack modContentPack = LoadedModManager.RunningModsListForReading.FirstOrDefault((ModContentPack p) => modMetaData?.SamePackageId(p.PackageId, false) ?? false);
			if (modMetaData != null)
			{
				foreach(ModMetaData mod in mods)
				{
					foreach(Type type in interfaceImplementations)
					{
						try
						{
							IConditionalPatch patch = (IConditionalPatch)Activator.CreateInstance(type, null);
							if(Utilities.MatchingPackage(mod.PackageId, patch.PackageId))
							{
								ModPatchable newMod = new ModPatchable()
								{
									PackageId = mod.PackageId,
									FriendlyName = mod.Name,
									Active = true,
									Patched = true
								};
						
								patch.PatchAll(newMod, harmony);

								SmashLog.Message($"[{modContentPack.Name}] Successfully applied compatibility patches for <mod>{mod.Name}</mod>");
								patchableModActivators.Add(new Pair<string, string>(sourcePackageID, mod.PackageId), newMod);
							}
						}
						catch (Exception ex)
						{
							SmashLog.Error($"{ProjectSetup.ProjectLabel} Failed to apply patch for {sourcePackageID} of type <type>{type}</type> for <mod>{mod.PackageId}</mod>. Exception={ex.Message}");
						}
					}
				}
			}
			
			return (modMetaData, modContentPack);
		}
	}
}
