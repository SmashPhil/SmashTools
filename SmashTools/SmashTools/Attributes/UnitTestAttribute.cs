using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Method)]
	public class UnitTestAttribute : Attribute
	{
		/// <summary>
		/// Queue method for unit testing
		/// </summary>
		public bool Active { get; set; } = false;
	}
}
