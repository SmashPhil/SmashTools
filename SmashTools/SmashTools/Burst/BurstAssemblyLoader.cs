using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unity.Burst;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Burst;

[PublicAPI]
public static class BurstAssemblyLoader
{
	private const string AssemblyFolder = "Burst";

	private const string FolderNameWindows = "Windows";
	private const string FolderNameLinux = "Linux";
	private const string FolderNameMac = "Mac";
	
	private const string FolderName64 = "x86_64";
	private const string FolderName32 = "x86";
	private const string FolderNameArm = "arm64";

	// TODO - I don't think Unity supports 32bit ARM, but I can't find any documentation on this.  Will check later
	private static bool IsArmArchitecture => 
		RuntimeInformation.ProcessArchitecture is Architecture.Arm64;

	private static string PlatformFolder
	{
		get
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return System.IO.Path.Combine(FolderNameWindows, UnityData.Is64BitBuild ? FolderName64 : FolderName32);

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        return FolderNameLinux;

      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return System.IO.Path.Combine(FolderNameMac, IsArmArchitecture ? FolderNameArm : FolderName64);

      if (Prefs.DevMode)
      {
        Log.Message(
          $"{RuntimeInformation.OSDescription} is not currently supported for burst lib.");
      }
			return null;
		}
	}

	private static bool IsValidFileExtension(string extension)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return extension == ".dll";
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			return extension == ".so";
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			return extension == ".bundle" || extension == ".dylib";

		return false;
	}

	/// <summary>
	/// Loads burst compiled assemblies at runtime.
	/// </summary>
	/// <param name="mod">Mod to load burst assemblies for.</param>
	public static bool LoadAllFor(ModContentPack mod)
	{
		string folderName = PlatformFolder;
		if (folderName.NullOrEmpty())
			return false;

		bool anyLoaded = false;
		string folderPath = System.IO.Path.Combine(AssemblyFolder, folderName);
		List<string> folders = mod.foldersToLoadDescendingOrder;
		for (int i = folders.Count; --i >= 0;)
		{
			string folder = folders[i];
			DirectoryInfo directoryInfo = new(System.IO.Path.Combine(folder, folderPath));
      if (!directoryInfo.Exists)
        continue;

      foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
      {
        try
        {
          Assert.IsTrue(IsValidFileExtension(fileInfo.Extension.ToLowerInvariant()));
          if (!BurstRuntime.LoadAdditionalLibrary(fileInfo.FullName))
          {
            Log.Error($"Unable to load file {fileInfo.Name} into BurstRuntime.");
            return false;
          }
          anyLoaded = true;
          if (Prefs.LogVerbose)
          {
            Log.Message($"[{mod.Name}] Loading {fileInfo.Name}");
          }
        }
        catch (Exception ex)
        {
          Log.Error($"Exception thrown loading {fileInfo.Name} into BurstRuntime.\n{ex}");
        }
      }
    }
		return anyLoaded;
	}
}