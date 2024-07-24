using RimWorld;
using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
	public static class AnimationLoader
	{
		public const string AnimationFolderName = "Animations";
		public const string DefaultAnimName = "New-Animation";
		public const string DefaultControllerName = "New-Controller";

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

		public static T LoadFile<T>(string filePath) where T : IAnimationFile, new()
		{
			T file = DirectXmlLoader.ItemFromXmlFile<T>(filePath);
			if (file == null)
			{
				Log.Error($"Could not load animation at \"{filePath}\". Path not found.");
				return default;
			}
			file.FilePath = filePath;
			file.FileName = Path.GetFileNameWithoutExtension(filePath);
			return file;
		}

		public static FileInfo CreateEmptyAnimFile(DirectoryInfo directoryInfo)
		{
			string fileName = GetAvailableFileName(directoryInfo, DefaultAnimName, AnimationClip.FileExtension);
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

		public static FileInfo CreateEmptyControllerFile(DirectoryInfo directoryInfo)
		{
			string fileName = GetAvailableFileName(directoryInfo, DefaultControllerName, AnimationController.FileExtension);
			string fileNameWithExtension = fileName + AnimationController.FileExtension;
			string filePath = Path.Combine(directoryInfo.FullName, fileNameWithExtension);
			AnimationController controller = AnimationController.EmptyController();
			controller.FileName = fileName;
			controller.FilePath = filePath;
			if (ExportControllerXml(controller))
			{
				return new FileInfo(filePath);
			}
			return null;
		}

		public static bool ExportAnimationXml(AnimationClip animationClip)
		{
			if (!animationClip)
			{
				return false;
			}
			bool exported = true;
			try
			{
				XmlExporter.StartDocument(animationClip.FilePath);
				XmlExporter.WriteElement(nameof(AnimationClip), animationClip);
			}
			catch (IOException ex)
			{
				exported = false;
				Log.Error($"Unable to export animation data.\nException = {ex}");
				Messages.Message($"Failed to save {animationClip.FileName}.", MessageTypeDefOf.RejectInput);
			}
			finally
			{
				XmlExporter.Close();
			}

			if (exported)
			{
				Messages.Message($"{animationClip.FileName} successfully saved at {animationClip.FilePath}", MessageTypeDefOf.TaskCompletion);
			}
			return exported;
		}

		public static bool ExportControllerXml(AnimationController controller)
		{
			if (!controller)
			{
				return false;
			}
			bool exported = true;
			try
			{
				XmlExporter.StartDocument(controller.FilePath);
				XmlExporter.WriteElement(nameof(AnimationController), controller);
			}
			catch (IOException ex)
			{
				exported = false;
				Log.Error($"Unable to export controller data.\nException = {ex}");
				Messages.Message($"Failed to save {controller.FileName}.", MessageTypeDefOf.RejectInput);
			}
			finally
			{
				XmlExporter.Close();
			}

			if (exported)
			{
				Messages.Message($"{controller.FileName} successfully saved at {controller.FilePath}", MessageTypeDefOf.TaskCompletion);
			}
			return exported;
		}

		public static List<FileInfo> GetAnimationClipFileInfo(ModContentPack modContentPack)
		{
			return GetFiles(modContentPack, AnimationClip.FileExtension);
		}

		public static List<FileInfo> GetAnimationControllerFileInfo(ModContentPack modContentPack)
		{
			return GetFiles(modContentPack, AnimationController.FileExtension);
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

		public static string GetAvailableFileName(DirectoryInfo directoryInfo, string defaultName, string fileExtension)
		{
			string name = defaultName;
			for (int i = 0; i < 100; i++)
			{
				bool result = true;
				foreach (FileInfo fileInfo in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
				{
					if (fileInfo.Name == name + fileExtension)
					{
						result = false;
						break;
					}
				}

				if (result)
				{
					return name;
				}
				name = $"{defaultName}({i})";
			}
			return $"{defaultName}-{Rand.Range(10000, 99999)}";
		}

		public static string GetAvailableName(IEnumerable<string> takenNames, string defaultName)
		{
			string name = defaultName;
			for (int i = 0; i < 100; i++)
			{
				bool result = true;
				foreach (string takenName in takenNames)
				{
					if (takenName == name)
					{
						result = false;
						break;
					}
				}

				if (result)
				{
					return name;
				}
				name = $"{defaultName} {i}";
			}
			return $"{defaultName} {Rand.Range(10000, 99999)}";
		}
	}
}
