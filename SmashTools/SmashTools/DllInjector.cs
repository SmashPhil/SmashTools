using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CoreLib.Burst;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

[PublicAPI]
public sealed class DllInjector
{
  private const string NativeAssemblyFolder = "Assemblies-Native";

  private const string MonoWindows = "mono-2.0-bdwgc";
  private const string MonoLinux = "libmonobdwgc-2.0.so";
  private const string MonoOSX = "libmonobdwgc-2.0.dylib";

  public static bool IsValidFileExtension(string extension)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return extension == ".dll";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      return extension == ".so";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return extension == ".dylib";

    return false;
  }

  public static void LoadAllNativeAssemblies(ModContentPack mod)
  {
    string folderName = OSUtils.GetPlatformFolder(OSUtils.GetOSPlatform());
    if (folderName.NullOrEmpty())
    {
      if (Prefs.DevMode)
      {
        Log.Warning($"{RuntimeInformation.OSDescription} is not supported for dll injection.");
      }
      return;
    }

    string folderPath = Path.Combine(NativeAssemblyFolder, folderName);
    List<string> folders = mod.foldersToLoadDescendingOrder;
    for (int i = folders.Count; --i >= 0;)
    {
      string folder = folders[i];
      DirectoryInfo directoryInfo = new(Path.Combine(folder, folderPath));
      if (!directoryInfo.Exists)
        continue;

      foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
      {
        try
        {
          Assert.IsTrue(IsValidFileExtension(fileInfo.Extension.ToLowerInvariant()));
          LoadDll(Path.GetFileNameWithoutExtension(fileInfo.Name), fileInfo.FullName);
        }
        catch (Exception ex)
        {
          Log.Error($"Exception thrown loading {fileInfo.Name} into BurstRuntime.\n{ex}");
        }
      }
    }
  }

  public static bool LoadDll(string name, string filePath)
  {
    FileInfo file = new(filePath);
    if (!file.Exists)
      return false;

    Log.Message($"Loading {name} at {filePath}");
    if (Prefs.LogVerbose)
    {
      Log.Message($"Loading {name} at {filePath}");
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      mono_dllmap_insert_windows(IntPtr.Zero, name, null, filePath, null);
      return true;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      mono_dllmap_insert_linux(IntPtr.Zero, name, null, filePath, null);
      return true;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      mono_dllmap_insert_osx(IntPtr.Zero, name, null, filePath, null);
      return true;
    }

    Log.Error("Platform not detected.");
    return false;
  }

  [DllImport(MonoWindows, EntryPoint = "mono_dllmap_insert")]
  private static extern void mono_dllmap_insert_windows(IntPtr assembly, string dll, string func, string tdll, string tfunc);

  [DllImport(MonoLinux, EntryPoint = "mono_dllmap_insert")]
  private static extern void mono_dllmap_insert_linux(IntPtr assembly, string dll, string func, string tdll, string tfunc);

  [DllImport(MonoOSX, EntryPoint = "mono_dllmap_insert")]
  private static extern void mono_dllmap_insert_osx(IntPtr assembly, string dll, string func, string tdll, string tfunc);
}
