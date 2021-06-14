using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Verse;
using RimWorld;

namespace SmashTools.Xml
{
	public static class FilePaths
	{
		public static List<string> ModFoldersForVersion(ModContentPack mod)
		{
			ModMetaData metaData = ModLister.GetModWithIdentifier(mod.PackageId);
			List<LoadFolder> loadFolders = new List<LoadFolder>();
			if ((metaData?.loadFolders) != null && metaData.loadFolders.DefinedVersions().Count > 0)
			{
				loadFolders = metaData.LoadFoldersForVersion(VersionControl.CurrentVersionStringWithoutBuild);
				if (!loadFolders.NullOrEmpty())
				{
					return loadFolders.Select(lf => lf.folderName).ToList();
				}
			}

			loadFolders = new List<LoadFolder>();

			int num = VersionControl.CurrentVersion.Major;
			int num2 = VersionControl.CurrentVersion.Minor;
			do
			{
				if (num2 == 0)
				{
					num--;
					num2 = 9;
				}
				else
				{
					num2--;
				}
				if (num < 1)
				{
					loadFolders = metaData.LoadFoldersForVersion("default");
					if (loadFolders != null)
					{
						return loadFolders.Select(lf => lf.folderName).ToList();
					}
					return DefaultFoldersForVersion(mod).ToList();
				}
				loadFolders = metaData.LoadFoldersForVersion(num + "." + num2);
			}
			while (loadFolders.NullOrEmpty());
			return loadFolders.Select(lf => lf.folderName).ToList();
		}

		public static IEnumerable<string> DefaultFoldersForVersion(ModContentPack mod)
		{
			ModMetaData metaData = ModLister.GetModWithIdentifier(mod.PackageId);

			string rootDir = mod.RootDir;
			string text = Path.Combine(rootDir, VersionControl.CurrentVersionStringWithoutBuild);
			if (Directory.Exists(text))
			{
				yield return text;
			}
			else
			{
				Version version = new Version(0, 0);
				DirectoryInfo[] directories = metaData.RootDir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					Version version2;
					if (VersionControl.TryParseVersionString(directories[i].Name, out version2) && version2 > version)
					{
						version = version2;
					}
				}
				if (version.Major > 0)
				{
					yield return Path.Combine(rootDir, version.ToString());
				}
			}
			string text2 = Path.Combine(rootDir, ModContentPack.CommonFolderName);
			if (Directory.Exists(text2))
			{
				yield return text2;
			}
			yield return rootDir;
		}
	}
}
