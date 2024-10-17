using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmashTools;

namespace SmashTools.Debugging
{
	public abstract class UnitTest
	{
		public abstract TestType ExecuteOn { get; }

		public abstract string Name { get; }

		public abstract IEnumerable<UTResult> Execute();

		protected UTResult True => new UTResult(string.Empty, true);

		protected UTResult False => new UTResult(string.Empty, false);

		[Flags]
		public enum TestType
		{
			None = 0,
			MainMenu = 1 << 0,
			GameLoaded = 1 << 1,
		}
	}

	public struct UTResult
	{
		public UTResult(string name, bool passed)
		{
			string adjustedName = !name.NullOrEmpty() ? $"{name} = " : string.Empty;
			Results = new List<(string name, bool result)>() { (adjustedName, passed) };
		}

		public List<(string name, bool result)> Results { get; private set; }

		public void Add(string name, bool passed)
		{
			Results ??= new List<(string name, bool result)>();
			Results.Add((name, passed));
		}

		public static UTResult For(string name, bool passed)
		{
			return new UTResult(name, passed);
		}
	}
}
