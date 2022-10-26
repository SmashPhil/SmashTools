using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public class Toggle
	{
		private readonly Func<bool> get_State;
		private readonly Action<bool> set_State;

		private readonly Action<bool> onToggle;

		public Toggle(string id, Func<bool> stateGetter, Action<bool> stateSetter, Action<bool> onToggle = null)
		{
			Id = id;
			DisplayName = id;
			Category = string.Empty;
			get_State = stateGetter;
			set_State = stateSetter;
			this.onToggle = onToggle;
		}

		public Toggle(string id, string category, Func<bool> stateGetter, Action<bool> stateSetter, Action<bool> onToggle = null)
		{
			Id = id;
			DisplayName = id;
			Category = category;
			get_State = stateGetter;
			set_State = stateSetter;
			this.onToggle = onToggle;
		}

		public Toggle(string id, string name, string category, Func<bool> stateGetter, Action<bool> stateSetter, Action<bool> onToggle = null)
		{
			Id = id;
			DisplayName = name;
			Category = category;
			get_State = stateGetter;
			set_State = stateSetter;
			this.onToggle = onToggle;
		}

		public string Id { get; private set; }
		public string DisplayName { get; private set; }
		public string Category { get; private set; }

		public bool Active { get => get_State(); set => set_State(value); }

		public void OnToggle(bool value) => onToggle?.Invoke(value);
	}
}
