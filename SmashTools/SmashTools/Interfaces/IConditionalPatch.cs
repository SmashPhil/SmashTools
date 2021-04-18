using HarmonyLib;

namespace SmashTools
{
    public interface IConditionalPatch
    {
        void PatchAll(ModPatchable mod, Harmony instance);

        string PackageId { get; }
    }
}
