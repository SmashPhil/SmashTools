using System;

namespace SmashTools;

public class ModVersion
{
  public ModVersion(int major, int minor, DateTime buildDate, DateTime startDate)
  {
    BuildDate = buildDate;
    int revision = (buildDate.Hour * 360 + buildDate.Minute * 60 + buildDate.Second) / 2; //AssemblyVersion.Revision is 1/2 the number of seconds into the day
    int patch = Math.Abs((buildDate - startDate).Days);
    Version = new Version(major, minor, patch, revision);

    VersionString = $"{Version.Major}.{Version.Minor}.{Version.Build}";
    VersionStringWithRevision = $"{Version.Major}.{Version.Minor}.{Version.Build} rev{Version.Revision}";
    VersionStringWithoutBuild = $"{Version.Major}.{Version.Minor}";
  }

  public Version Version { get; private set; }

  public DateTime BuildDate { get; private set; }

  public string VersionString { get; private set; }

  public string VersionStringWithoutBuild { get; private set; }

  public string VersionStringWithRevision { get; private set; }
}