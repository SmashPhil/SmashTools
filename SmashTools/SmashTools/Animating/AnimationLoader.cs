using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Animations
{
	public static class AnimationLoader
	{
		public const string AnimationFolderName = "Animations";

		public static List<FileInfo> GetAnimationClipFileInfo(ModContentPack modContentPack)
		{
			return GetFiles(modContentPack, AnimationClip.FileExtension);
		}

		private static List<FileInfo> GetFiles(ModContentPack modContentPack, string fileExtension)
		{
			List<FileInfo> files = new List<FileInfo>();
			List<string> loadFolders = FilePaths.ModFoldersForVersion(modContentPack);
			try
			{
				foreach (string folder in loadFolders)
				{
					string assetDirectory = Path.Combine(modContentPack.RootDir, folder, AnimationFolderName);
					DirectoryInfo directoryInfo = new DirectoryInfo(assetDirectory);
					if (directoryInfo.Exists)
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
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Unable to load AssetBundle.\nException = {ex}\nFoldersSearched={loadFolders.ToCommaList()}");
			}
			return files;
		}
	}
}
