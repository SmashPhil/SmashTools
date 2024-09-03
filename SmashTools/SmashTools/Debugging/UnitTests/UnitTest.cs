using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Debugging
{
	public abstract class UnitTest
	{
		public abstract string Name { get; }

		public abstract IEnumerable<UTResult> Execute();

		protected UTResult True => new UTResult(string.Empty, true);

		protected UTResult False => new UTResult(string.Empty, false);
	}

	public readonly struct UTResult
	{
		public UTResult(string name, bool passed)
		{
			Name = !name.NullOrEmpty() ? $"{name}=" : string.Empty;
			Passed = passed;
		}

		public string Name { get; }

		public bool Passed { get; }

		public static UTResult For(string name, bool passed) => new UTResult(name, passed);
	}
}
