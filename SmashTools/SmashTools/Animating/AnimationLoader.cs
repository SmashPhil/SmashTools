using RimWorld;
using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
	public static class AnimationLoader
	{
		public const string AnimationFolderName = "Animations";
		public const string DefaultName = "New-Animation";

		public static DirectoryInfo AnimationDirectory(ModContentPack modContentPack)
		{
			List<string> loadFolders = FilePaths.ModFoldersForVersion(modContentPack);
			foreach (string folder in loadFolders)
			{
				string assetDirectory = Path.Combine(modContentPack.RootDir, folder, AnimationFolderName);
				DirectoryInfo directoryInfo = new DirectoryInfo(assetDirectory);
				if (directoryInfo.Exists)
				{
					return directoryInfo;
				}
			}
			return null;
		}

		public static AnimationClip LoadAnimation(string filePath)
		{
			AnimationClip animationClip = DirectXmlLoader.ItemFromXmlFile<AnimationClip>(filePath);
			if (animationClip == null)
			{
				Log.Error($"Could not load animation at \"{filePath}\". Path not found.");
				return null;
			}
			animationClip.FilePath = filePath;
			animationClip.FileName = Path.GetFileName(filePath);
			return animationClip;
		}

		public static FileInfo CreateEmptyAnimFile(DirectoryInfo directoryInfo)
		{
			string fileName = GetAvailableDefaultName(directoryInfo);
			string fileNameWithExtension = fileName + AnimationClip.FileExtension;
			string filePath = Path.Combine(directoryInfo.FullName, fileNameWithExtension);
			AnimationClip animationClip = new AnimationClip();
			animationClip.FileName = fileName;
			animationClip.FilePath = filePath;
			if (ExportAnimationXml(animationClip))
			{
				return new FileInfo(filePath);
			}
			return null;
		}

		public static bool ExportAnimationXml(AnimationClip animationClip)
		{
			if (animationClip == null)
			{
				return false;
			}
			bool exported = true;
			try
			{
				DirectXmlSaver.SaveDataObject(animationClip, animationClip.FilePath);
			}
			catch (Exception ex)
			{
				exported = false;
				Log.Error($"Unable to export animation data. Exception = {ex}");
			}

			if (exported)
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			else
			{
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			}
			return exported;
		}

		public static List<FileInfo> GetAnimationClipFileInfo(ModContentPack modContentPack)
		{
			return GetFiles(modContentPack, AnimationClip.FileExtension);
		}

		private static List<FileInfo> GetFiles(ModContentPack modContentPack, string fileExtension)
		{
			List<FileInfo> files = new List<FileInfo>();
			try
			{
				DirectoryInfo directoryInfo = AnimationDirectory(modContentPack);
				if (directoryInfo != null && directoryInfo.Exists)
				{
					foreach (FileInfo fileInfo in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
					{
						if (fileInfo.Extension == fileExtension)
						{
							files.Add(fileInfo);
						}
					}
				}
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Unable to load AssetBundle.\nException = {ex}");
			}
			return files;
		}

		private static string GetAvailableDefaultName(DirectoryInfo directoryInfo)
		{
			string name = DefaultName;
			for (int i = 0; i < 100; i++)
			{
				bool result = true;
				foreach (FileInfo fileInfo in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
				{
					if (fileInfo.Name == name + AnimationClip.FileExtension)
					{
						result = false;
						break;
					}
				}

				if (result)
				{
					return name;
				}
				name = $"{DefaultName}({i})";
			}
			return $"{DefaultName}-{Rand.Range(10000, 99999)}";
		}
	}
}
