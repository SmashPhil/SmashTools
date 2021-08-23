using System;
using System.IO;
using System.Reflection;
using Verse;

namespace SmashTools
{
	public static class Utilities
	{
		/// <summary>
		/// Action delegates with pass-by-reference parameters allowed
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item"></param>
		public delegate void ActionRef<T>(ref T item);
		public delegate void ActionRefP1<T1, T2>(ref T1 item1, T2 item2);
		public delegate void ActionRefP2<T1, T2>(T1 item1, ref T2 item2);
		public delegate void ActionRef<T1, T2>(ref T1 item1, ref T2 item2);

		/// <summary>
		/// Delete Mod config file
		/// </summary>
		/// <param name="modContentPack"></param>
		/// <param name="mod"></param>
		public static void DeleteConfig(Mod mod)
		{
			string settingsFileName = Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename(string.Format("Mod_{0}_{1}.xml", mod.Content.FolderName, mod.GetType().Name)));
			if (File.Exists(settingsFileName))
			{
				File.Delete(settingsFileName);
			}
		}

		/// <summary>
		/// Delete <see cref="SmashSettings"/>
		/// </summary>
		internal static void DeleteSettings()
		{
			string filePath = SmashSettings.FullPath;
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}

		/// <summary>
		/// Invoke anonymous method with logging. Useful for quick method execution without having to set up many try catch blocks.
		/// </summary>
		/// <param name="action"></param>
		public static void InvokeWithLogging(this Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Unable to execute {action.Method?.Name ?? action.ToString()} Exception={ex.Message}");
			}
		}
	}
}
