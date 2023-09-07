using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class ConditionalPatches
	{
		private static Dictionary<Type, ConditionalPatch.Results> patches = new Dictionary<Type, ConditionalPatch.Results>();

		/// <summary>
		/// Apply all conditional patches for a mod
		/// </summary>
		static ConditionalPatches()
		{
			Harmony harmony = ProjectSetup.Harmony;
			
			List<Type> conditionalPatchTypes = GenTypes.AllSubclassesNonAbstract(typeof(ConditionalPatch));

			foreach (Type type in conditionalPatchTypes)
			{
				try
				{
					ConditionalPatch patch = (ConditionalPatch)Activator.CreateInstance(type, null);
					ConditionalPatch.Results result = new ConditionalPatch.Results()
					{
						PackageId = patch.PackageId
					};
					if (ModLister.GetActiveModWithIdentifier(patch.PackageId, ignorePostfix: true) is ModMetaData modMetaData)
					{
						result.FriendlyName = modMetaData.Name;
						
						patch.PatchAll(modMetaData, harmony);

						result.Active = true;

						SmashLog.Message($"[{patch.SourceId}] Successfully applied compatibility patches for <mod>{modMetaData.Name}</mod>");
					}
					patches[type] = result;
				}
				catch (Exception ex)
				{
					SmashLog.Error($"{ProjectSetup.ProjectLabel} Failed to apply patch <type>{type}</type>. Exception={ex}");
				}
			}
		}

		public static ConditionalPatch.Results PatchResult<T>() where T : ConditionalPatch
		{
			return patches.TryGetValue(typeof(T), ConditionalPatch.Results.Invalid);
		}

		public static bool ActivePatch<T>() where T : ConditionalPatch
		{
			return PatchResult<T>().Active;
		}
	}
}
