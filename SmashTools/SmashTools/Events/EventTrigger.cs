using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public class EventTrigger
	{
		private List<Action> persistents;
		private List<Action> singles;

		public EventTrigger()
		{
			persistents = new List<Action>();
			singles = new List<Action>();
		}

		public void Add(Action action) => persistents.Add(action);

		public void AddSingle(Action action) => singles.Add(action);

		public bool Remove(Action action) => persistents.Remove(action);

		public bool RemoveSingle(Action action) => singles.Remove(action);

		public void ExecuteEvents()
		{
			foreach (Action action in persistents)
			{
				action();
			}
			for (int i = singles.Count - 1; i >= 0; i--)
			{
				Action action = singles[i];
				action();
				singles.Remove(action);
			}
		}
	}
}
