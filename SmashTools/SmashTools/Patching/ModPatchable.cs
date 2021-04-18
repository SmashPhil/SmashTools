using System;
using System.Linq;
using Verse;

namespace SmashTools
{
    public class ModPatchable
    {
        public string PackageId { get; set; }
        public string FriendlyName { get; set; }
        public bool Active { get; set; }
        public bool Patched { get; set; }
        public Exception ExceptionThrown { get; set; }

        public static ModPatchable GetModPatchable(string sourceModPackageId, string packageId)
        {
            ModPatchable mod = ConditionalPatches.patchableModActivators.SingleOrDefault(kvp => 
                kvp.Key.First.EqualsIgnoreCase(sourceModPackageId) && kvp.Key.Second.EqualsIgnoreCase(packageId)).Value;
            if (mod is null)
            {
                Log.Error($"Failed to retrieve \"{packageId}\" for patching.");
            }
            return mod;
        }
    }
}
