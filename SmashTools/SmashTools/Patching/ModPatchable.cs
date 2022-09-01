using System;
using System.Linq;
using Verse;

namespace SmashTools
{
	public class ModPatchable
	{
		public string PackageId { get; set; }
		public string FriendlyName { get; set; }
		public bool Active { get; set; }
		public bool Patched { get; set; }
		public Exception ExceptionThrown { get; set; }
	}
}
