﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SmashTools
{
	public static class ConditionalPatches
	{
		/// <summary>
		/// Apply all conditional patches for a mod
		/// </summary>
		/// <param name="harmony"></param>
		/// <param name="sourcePackageID"></param>
		public static void PatchAllActiveMods(Harmony harmony, string sourcePackageID)
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
							if (mod.SamePackageId(patch.PackageId))
							{
								ModPatchable newMod = new ModPatchable()
								{
									PackageId = mod.PackageId,
									FriendlyName = mod.Name,
									Active = true,
									Patched = true
								};
						
								patch.PatchAll(mod, harmony);

								if (type.GetProperty("Active", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) is PropertyInfo property && property.GetSetMethod() != null)
								{
									property.SetValue(null, true);
								}

								SmashLog.Message($"[{modContentPack.Name}] Successfully applied compatibility patches for <mod>{mod.Name}</mod>");
							}
						}
						catch (Exception ex)
						{
							SmashLog.Error($"{ProjectSetup.ProjectLabel} Failed to apply patch for {sourcePackageID} of type <type>{type}</type> for <mod>{mod.PackageId}</mod>. Exception={ex.Message}");
						}
					}
				}
			}
		}
	}
}
