using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public interface IPathfinder<T>
	{
		bool CanEnter(T current);

		IEnumerable<T> Neighbors(T current);

		int Cost(T from, T to);
	}
}
