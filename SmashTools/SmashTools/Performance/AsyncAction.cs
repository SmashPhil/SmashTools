using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Performance
{
	public class AsyncAction
	{
		public Action action;
		private Func<bool> validator;

		public AsyncAction(Action action, Func<bool> validator = null)
		{
			this.action = action;
			this.validator = validator;
		}

		public bool IsValid => validator is null || validator();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke() => action.Invoke();

		public static implicit operator AsyncAction(Action action)
		{
			return new AsyncAction(action);
		}

		public override string ToString()
		{
			return action.Method.Name;
		}
	}
}
