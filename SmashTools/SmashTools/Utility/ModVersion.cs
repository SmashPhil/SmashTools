using System;
using System.Reflection;
using Verse;

namespace SmashTools
{
	public class ModVersion
	{
		public ModVersion(Version version, DateTime startDate)
		{
			BuildDate = new DateTime(2000, 1, 1).AddDays(version.Build);
			int revision = version.Revision * 2 / 60;
			int build = Math.Abs((DateTime.Now - startDate).Days);
			Version = new Version(version.Major, version.Minor, build, revision);

			VersionString = $"{Version.Major}.{Version.Minor}.{Version.Build}";
			VersionStringWithRevision = $"{Version.Major}.{Version.Minor}.{Version.Build} rev{Version.Revision}";
			VersionStringWithoutBuild = $"{Version.Major}.{Version.Minor}";
		}

		public Version Version { get; private set; }

		public DateTime BuildDate { get; private set; }

		public string VersionString { get; private set; }

		public string VersionStringWithoutBuild { get; private set; }

		public string VersionStringWithRevision { get; private set; }

		public static ModVersion VersionFromAssembly(Assembly assembly, DateTime startDate)
		{
			Version version = assembly.GetName().Version;
			return new ModVersion(version, startDate);
		}
	}
}
