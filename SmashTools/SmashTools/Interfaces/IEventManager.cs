using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public interface IEventManager<T>
	{
		Dictionary<T, EventTrigger> EventRegistry { get; set; }
	}
}
