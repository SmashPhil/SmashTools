using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using SmashTools;
using SmashTools.Xml;
using Verse;

namespace SmashTools.Animations
{
	[StaticConstructorOnModInit]
	public static class AnimationLoader
	{
		public const string AnimationFolderName = "Animations";
		public const string AnimationFolder = AnimationFolderName + "/";

		private static readonly Dictionary<Type, string> fileExtensions = new Dictionary<Type, string>
		{
			{ typeof(AnimationClip), AnimationClip.FileExtension },
			{ typeof(AnimationController), AnimationController.FileExtension }
		};

		static AnimationLoader()
		{
			ParseHelper.Parsers<AnimationClip>.Register(ParseAnimationFileByGuid<AnimationClip>);
			ParseHelper.Parsers<AnimationController>.Register(ParseAnimationFileByPath<AnimationController>);

			LoadAll();
		}

		internal static void LoadAll()
		{
			foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
			{
				LoadAnimationFiles<AnimationClip>(mod);
				LoadAnimationFiles<AnimationController>(mod);
			}
		}
		
		private static void LoadAnimationFiles<T>(ModContentPack mod) where T : IAnimationFile, new()
		{
			Dictionary<string, FileInfo> allFilesForMod = ModContentPack.GetAllFilesForMod(mod, AnimationFolder, IsAcceptableExtension<T>);
			foreach ((string path, FileInfo fileInfo) in allFilesForMod)
			{
				T file = LoadFile<T>(fileInfo.FullName);
				string relativePath = path;
				if (relativePath.StartsWith(AnimationFolder))
				{
					relativePath = path.Substring(AnimationFolder.Length);
				}
				if (Path.HasExtension(relativePath))
				{
					relativePath = Path.GetFileNameWithoutExtension(relativePath);
				}
				Cache<T>.Add(relativePath, file);
			}
		}

		private static bool IsAcceptableExtension<T>(string ext)
		{
			return fileExtensions.TryGetValue(typeof(T), out string fileExt) && ext == fileExt;
		}

		private static T ParseAnimationFileByPath<T>(string filePath) where T : IAnimationFile, new()
		{
			return LoadFile<T>(filePath);
		}

		private static T ParseAnimationFileByGuid<T>(string guidStr) where T : IAnimationFile, new()
		{
			if (Guid.TryParse(guidStr, out Guid guid) && Cache<T>.Get(guid, out T file))
			{
				return file;
			}
			Log.Error($"Unable to load animation file {guidStr}");
			return default;
		}

		public static T LoadFile<T>(string filePath) where T : IAnimationFile, new()
		{
			if (Cache<T>.Get(filePath, out T file))
			{
				return file;
			}
			if (!File.Exists(filePath))
			{
				Log.Error($"Unable to load file at \"{filePath}\". File not found.");
				return default;
			}
			file = DirectXmlLoader.ItemFromXmlFile<T>(filePath);
			if (file == null)
			{
				Log.Error($"Unable to load animation file at \"{filePath}\".");
				return default;
			}
			file.FilePath = filePath;
			file.FileName = Path.GetFileNameWithoutExtension(filePath);
			file.ResolveReferences();
			return file;
		}

		/// <returns>True if AnimationClip saved to path without need for file picker dialog.</returns>
		public static bool Save<T>(T file) where T : IAnimationFile, new()
		{
			if (file == null) return false;

			if (file.FilePath == null || !File.Exists(file.FilePath))
			{
				SaveAs(file);
				return false;
			}
			ExportXml(file);
			return true;
		}

		public static void SaveAs<T>(T file) where T : IAnimationFile, new()
		{
			if (file == null) return;

			Dialog_FilePicker filePicker = new Dialog_FilePicker(("Save".Translate(), (dir) => ExportXmlToDirectory(file, dir)));
			Find.WindowStack.Add(filePicker);
		}

		private static void ExportXmlToDirectory<T>(T file, DirectoryInfo directory) where T : IAnimationFile, new()
		{
			file.FilePath = Path.Combine(directory.FullName, file.FileNameWithExtension);
			ExportXml(file);
		}

		private static void ExportXml<T>(T file) where T : IAnimationFile, new()
		{
			bool exported = true;
			try
			{
				XmlExporter.StartDocument(file.FilePath);
				XmlExporter.WriteElement(file.GetType().Name, file);
			}
			catch (IOException ex)
			{
				exported = false;
				Log.Error($"Unable to export animation data.\nException = {ex}");
				Messages.Message($"Failed to save {file.FileName}.", MessageTypeDefOf.RejectInput);
			}
			finally
			{
				XmlExporter.Close();
			}

			if (exported)
			{
				Messages.Message($"{file.FileName} successfully saved at {file.FilePath}", MessageTypeDefOf.TaskCompletion);
			}
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
			return $"{defaultName} {Rand.Range(100000, 999999)}";
		}

		internal static class Cache<T> where T : IAnimationFile
		{
			private static readonly Dictionary<string, T> files = new Dictionary<string, T>();
			private static readonly Dictionary<Guid, T> filesByGuid = new Dictionary<Guid, T>();

			public static int Count => files.Count;

			public static List<T> GetAll() => files.Values.ToList();

			public static void Add(string path, T file)
			{
				files[path] = file;
				filesByGuid[file.Guid] = file;
			}

			public static bool Get(string path, out T file)
			{
				return files.TryGetValue(path, out file);
			}

			public static bool Get(Guid guid, out T file)
			{
				return filesByGuid.TryGetValue(guid, out file);
			}
		}
	}
}
