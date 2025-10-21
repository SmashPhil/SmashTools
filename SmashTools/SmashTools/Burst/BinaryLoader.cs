using System;
using System.IO;
using JetBrains.Annotations;
using Unity.Burst;
using Verse;

namespace SmashTools.Burst;

[PublicAPI]
public static class BinaryLoader
{
	private const string AssemblyFolder = "AssembliesBurst/";

	/// <summary>
	/// Loads burst compiled assemblies at runtime.
	/// </summary>
	/// <param name="mod">Mod to load burst assemblies for.</param>
	public static void LoadAll(ModContentPack mod)
	{
		foreach ((_, FileInfo fileInfo) in ModContentPack.GetAllFilesForModPreserveOrder(mod, AssemblyFolder, 
			static ext => ext.ToLowerInvariant() == "*.dll"))
		{
			try
			{
				if (!BurstRuntime.LoadAdditionalLibrary(fileInfo.FullName))
				{
					Log.Error($"Unable to load file {fileInfo.Name} into BurstRuntime.");
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown loading {fileInfo.Name} into BurstRuntime.\n{ex}");
			}
		}
	}
}