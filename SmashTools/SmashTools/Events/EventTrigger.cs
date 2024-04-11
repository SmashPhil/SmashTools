using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public class EventTrigger
	{
		private List<(string key, Action action)> persistents;
		private List<(string key, Action action)> singles;

		public EventTrigger()
		{
			persistents = new List<(string key, Action action)>();
			singles = new List<(string key, Action action)>();
		}

		public void Add(Action action, string key) => persistents.Add((key, action));

		public void Add(Action action) => persistents.Add((null, action));

		public void AddSingle(Action action, string key) => singles.Add((key, action));

		public void AddSingle(Action action) => singles.Add((null, action));

		public int Remove(string key)
		{
			int count = 0;
			for (int i = persistents.Count - 1; i >= 0; i--)
			{
				(string keyMatch, _) = persistents[i];
				if (keyMatch == key)
				{
					persistents.RemoveAt(i);
					count++;
				}
			}
			return count;
		}

		public int Remove(Action action)
		{
			int count = 0;
			for (int i = persistents.Count - 1; i >= 0; i--)
			{
				(_, Action actionMatch) = persistents[i];
				if (actionMatch == action)
				{
					persistents.RemoveAt(i);
					count++;
				}
			}
			return count;
		}

		public int RemoveSingle(string key)
		{
			int count = 0;
			for (int i = singles.Count - 1; i >= 0; i--)
			{
				(string keyMatch, _) = singles[i];
				if (keyMatch == key)
				{
					singles.RemoveAt(i);
					count++;
				}
			}
			return count;
		}

		public int RemoveSingle(Action action)
		{
			int count = 0;
			for (int i = singles.Count - 1; i >= 0; i--)
			{
				(_, Action actionMatch) = singles[i];
				if (actionMatch == action)
				{
					singles.RemoveAt(i);
					count++;
				}
			}
			return count;
		}

		public void ClearAll()
		{
			singles.Clear();
			persistents.Clear();
		}

		public void ExecuteEvents()
		{
			foreach ((_, Action action) in persistents)
			{
				action();
			}
			for (int i = singles.Count - 1; i >= 0; i--)
			{
				(_, Action action) = singles[i];
				action();
				singles.RemoveAt(i);
			}
		}
	}
}
