using Verse;
using HarmonyLib;

namespace SmashTools
{
	public interface IConditionalPatch
	{
		void PatchAll(ModMetaData mod, Harmony instance);

		string PackageId { get; }
	}
}
