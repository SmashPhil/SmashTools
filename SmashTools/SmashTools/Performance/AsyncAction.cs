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
		public readonly Action action;
		public readonly Func<bool> validator;
		public readonly Action<Exception> exceptionHandler;

		public AsyncAction(Action action, Func<bool> validator = null, Action<Exception> exceptionHandler = null)
		{
			this.action = action;
			this.validator = validator;
			this.exceptionHandler = exceptionHandler;
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
