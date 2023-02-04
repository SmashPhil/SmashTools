using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	/// <summary>
	/// Assigns sequential indices
	/// </summary>
	/// <typeparam name="T">Def type used for grouping indices</typeparam>
	public interface IDefIndex<T> where T : Def
	{
		int DefIndex { get; set; }
	}
}
